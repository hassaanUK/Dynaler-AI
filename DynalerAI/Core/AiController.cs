using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace DynalerAI.Core;

public class AiOptions
{
    public string Goal      { get; set; } = "";
    public int    ModeIndex { get; set; }
    public string Model     { get; set; } = "gpt-4o";
    public string ApiKey    { get; set; } = "";
    public bool   SafeMode     { get; set; }
    public bool   ScreenVision { get; set; }
    public bool   AutoRetry    { get; set; }
    public int    MaxRetries   { get; set; } = 2;
    public int    ActionDelayMs { get; set; } = 150;
}

public class AiController
{
    private readonly Action<string> _log;
    private readonly Action<string> _planUpdate;
    private readonly Action<string> _statusUpdate;
    private CancellationTokenSource _cts = new();
    private static readonly HttpClient _http = new();

    public AiController(Action<string> log, Action<string> planUpdate, Action<string> statusUpdate)
    {
        _log = log;
        _planUpdate = planUpdate;
        _statusUpdate = statusUpdate;
    }

    public void Stop() => _cts.Cancel();

    public async Task RunAsync(AiOptions opts)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        int attempt    = 0;
        int maxAttempts = opts.AutoRetry ? opts.MaxRetries + 1 : 1;

        while (attempt < maxAttempts && !token.IsCancellationRequested)
        {
            attempt++;
            if (attempt > 1) _log($"Retrying... (attempt {attempt}/{maxAttempts})");
            try
            {
                await ExecuteGoalAsync(opts, token);
                break;
            }
            catch (OperationCanceledException)
            {
                _log("Stopped by user.");
                break;
            }
            catch (Exception ex)
            {
                _log($"Error: {ex.Message}");
                if (attempt >= maxAttempts) _log("Max retries reached.");
            }
        }
    }

    private async Task ExecuteGoalAsync(AiOptions opts, CancellationToken token)
    {
        _log($"Goal: {opts.Goal}");
        _statusUpdate("Thinking...");

        string? screenshot = null;
        if (opts.ScreenVision)
        {
            screenshot = CaptureScreen();
            _log("Screen captured for AI vision.");
        }

        var plan = await GetPlanFromAiAsync(opts, screenshot, token);
        _planUpdate(plan);
        _log("Plan received. Executing...");

        var steps = ParseSteps(plan);
        foreach (var step in steps)
        {
            token.ThrowIfCancellationRequested();
            _log($"Step: {step}");
            _statusUpdate(step);

            if (opts.SafeMode)
            {
                // Use WPF MessageBox — no WinForms dependency needed here
                var result = MessageBox.Show(
                    $"AI wants to:\n\n{step}\n\nAllow?",
                    "Safe Mode — Confirm Action",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.No) { _log("Step skipped by user."); continue; }
            }

            await Executor.ExecuteStepAsync(step, opts.ActionDelayMs, token);
            await Task.Delay(opts.ActionDelayMs, token);
        }

        _log("Goal completed.");
        _statusUpdate("Done");
        _planUpdate("Completed ✓");
    }

    private async Task<string> GetPlanFromAiAsync(AiOptions opts, string? screenshot, CancellationToken token)
    {
        // Guard: built-in mode needs a key; custom modes need a key too
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
            throw new InvalidOperationException("API key is missing. Enter it in Settings.");

        const string systemPrompt =
            "You are Dynaler AI, a Windows desktop automation assistant.\n" +
            "The user gives you a goal. Respond ONLY with a numbered action plan like:\n" +
            "1. Click on [element]\n" +
            "2. Type [text]\n" +
            "3. Press [key]\n" +
            "Each step must be a single, concrete action. Maximum 10 steps.";

        var userMessage = screenshot != null
            ? $"Goal: {opts.Goal}\n\n[Screen context captured]"
            : $"Goal: {opts.Goal}";

        string endpoint, body;

        if (opts.ModeIndex <= 1) // OpenAI
        {
            endpoint = "https://api.openai.com/v1/chat/completions";
            body = JsonSerializer.Serialize(new
            {
                model    = opts.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userMessage  }
                },
                max_tokens = 500
            });
        }
        else if (opts.ModeIndex == 2) // Claude
        {
            endpoint = "https://api.anthropic.com/v1/messages";
            body = JsonSerializer.Serialize(new
            {
                model      = opts.Model,
                max_tokens = 500,
                system     = systemPrompt,
                messages   = new[] { new { role = "user", content = userMessage } }
            });
        }
        else // Gemini
        {
            endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{opts.Model}:generateContent?key={opts.ApiKey}";
            body = JsonSerializer.Serialize(new
            {
                contents = new[] { new { parts = new[] { new { text = systemPrompt + "\n\n" + userMessage } } } }
            });
        }

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (opts.ModeIndex <= 1) // OpenAI Bearer
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);
        }
        else if (opts.ModeIndex == 2) // Claude key + version header
        {
            request.Headers.Add("x-api-key", opts.ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
        }
        // Gemini: key is in the URL, no header needed

        var response = await _http.SendAsync(request, token);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(token);
            throw new HttpRequestException($"AI API returned {(int)response.StatusCode}: {err}");
        }

        var json = await response.Content.ReadAsStringAsync(token);
        return ExtractContent(json, opts.ModeIndex);
    }

    private static string ExtractContent(string json, int modeIdx)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (modeIdx <= 1)
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";
            else if (modeIdx == 2)
                return doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";
            else
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "";
        }
        catch { return "1. Could not parse AI response."; }
    }

    private static List<string> ParseSteps(string plan)
    {
        var steps = new List<string>();
        foreach (var line in plan.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 2 && char.IsDigit(trimmed[0]))
            {
                var idx = trimmed.IndexOf('.');
                if (idx > 0 && idx < trimmed.Length - 1)
                    steps.Add(trimmed[(idx + 1)..].Trim());
            }
        }
        return steps.Count > 0 ? steps : new List<string> { plan.Trim() };
    }

    private static string CaptureScreen()
    {
        try
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            using var bmp = new System.Drawing.Bitmap(bounds.Width, bounds.Height);
            using var g   = System.Drawing.Graphics.FromImage(bmp);
            g.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
            using var ms  = new System.IO.MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch { return ""; }
    }
}
