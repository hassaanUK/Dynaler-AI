using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Text.Json;
using DynalerAI.Core;

namespace DynalerAI;

public partial class MainWindow : Window
{
    private readonly AiController _aiController;
    private readonly ConfigManager _config;
    private readonly HotkeyManager _hotkey;
    private bool _running = false;

    public MainWindow()
    {
        InitializeComponent();
        _config      = new ConfigManager();
        _aiController = new AiController(Log, OnPlanUpdate, OnStatusUpdate);
        _hotkey      = new HotkeyManager(this, StopAi);

        LoadPresets();
        LoadConfig();
        // Hotkey is registered in HotkeyManager.OnLoaded after HWND exists
        RegisterHotkey();
    }

    private void LoadConfig()
    {
        var cfg = _config.Load();
        SafeModeCheck.IsChecked    = cfg.SafeMode;
        ScreenVisionCheck.IsChecked = cfg.ScreenVision;
        AutoRetryCheck.IsChecked   = cfg.AutoRetry;
    }

    private void RegisterHotkey()
    {
        _hotkey.Register(Key.S, ModifierKeys.Control | ModifierKeys.Shift);
    }

    // Title bar
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _hotkey.Unregister();
        Application.Current.Shutdown();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow(_config);
        win.Owner = this;
        win.ShowDialog();
        RegisterHotkey(); // Re-register in case hotkey changed
    }

    // AI Mode
    private void AiMode_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ApiKeyPanel == null) return;
        var idx = AiModeCombo.SelectedIndex;
        ApiKeyPanel.Visibility = idx > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (ModelCombo == null) return;
        ModelCombo.Items.Clear();
        switch (idx)
        {
            case 0: // Built-in ChatGPT
            case 1: // Custom OpenAI
                ModelCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "gpt-4o" });
                ModelCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "gpt-4o-mini" });
                ModelCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "gpt-4-turbo" });
                break;
            case 2: // Claude
                ModelCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "claude-3-5-sonnet-20241022" });
                ModelCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "claude-3-haiku-20240307" });
                break;
            case 3: // Gemini
                ModelCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "gemini-1.5-pro" });
                ModelCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "gemini-1.5-flash" });
                break;
        }
        ModelCombo.SelectedIndex = 0;
    }

    // Start / Stop
    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;
        var goal = GoalInput.Text.Trim();
        if (string.IsNullOrEmpty(goal)) { Log("Please enter a goal."); return; }

        var modeIdx = AiModeCombo.SelectedIndex;
        var model   = (ModelCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "gpt-4o";
        var apiKey  = ApiKeyBox.Password.Trim();
        var cfg     = _config.Load();

        // For built-in mode use stored key; for custom modes use what user typed
        var resolvedKey = modeIdx == 0 ? cfg.BuiltinKey : apiKey;

        if (string.IsNullOrWhiteSpace(resolvedKey))
        {
            var msg = modeIdx == 0
                ? "No built-in API key set. Go to Settings and enter your OpenAI key."
                : "Please enter your API key.";
            Log(msg);
            return;
        }

        _running = true;
        StartBtn.IsEnabled = false;
        StopBtn.IsEnabled  = true;
        SetStatus("Running", "#2ECC71");

        var options = new AiOptions
        {
            Goal         = goal,
            ModeIndex    = modeIdx,
            Model        = model,
            ApiKey       = resolvedKey,
            SafeMode     = SafeModeCheck.IsChecked    == true,
            ScreenVision = ScreenVisionCheck.IsChecked == true,
            AutoRetry    = AutoRetryCheck.IsChecked   == true,
            MaxRetries   = cfg.MaxRetries,
            ActionDelayMs = cfg.ActionDelayMs,
        };

        await _aiController.RunAsync(options);
        StopAi();
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => StopAi();

    public void StopAi()
    {
        _aiController.Stop();
        _running = false;
        Dispatcher.Invoke(() =>
        {
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled  = false;
            SetStatus("Idle", "#8B7BAE");
        });
    }

    // Logging
    public void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Dispatcher.Invoke(() =>
        {
            LogText.Text += "\n" + line;
            LogScroller.ScrollToEnd();
        });

        var cfg = _config.Load();
        if (cfg.EnableLogging)
            File.AppendAllText(ConfigManager.LogFile, line + "\n");
    }

    private void OnPlanUpdate(string plan)   => Dispatcher.Invoke(() => PlanText.Text = plan);
    private void OnStatusUpdate(string status) => Dispatcher.Invoke(() => SetStatus(status, "#6C3FC5"));

    private void SetStatus(string text, string color)
    {
        StatusText.Text = text;
        StatusDot.Fill  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    // Presets
    private List<string> _presets = new();

    private void LoadPresets()
    {
        if (File.Exists(ConfigManager.PresetsFile))
        {
            try { _presets = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(ConfigManager.PresetsFile)) ?? new(); }
            catch { _presets = new(); }
        }
        RefreshPresetsList();
    }

    private void SavePresets() =>
        File.WriteAllText(ConfigManager.PresetsFile, JsonSerializer.Serialize(_presets));

    private void RefreshPresetsList()
    {
        PresetsList.Items.Clear();
        foreach (var p in _presets) PresetsList.Items.Add(p);
    }

    private void SavePreset_Click(object sender, RoutedEventArgs e)
    {
        var goal = GoalInput.Text.Trim();
        if (string.IsNullOrEmpty(goal)) return;
        if (!_presets.Contains(goal))
        {
            _presets.Add(goal);
            SavePresets();
            RefreshPresetsList();
        }
    }

    private void DeletePreset_Click(object sender, RoutedEventArgs e)
    {
        if (PresetsList.SelectedItem is string selected)
        {
            _presets.Remove(selected);
            SavePresets();
            RefreshPresetsList();
        }
    }

    private void Preset_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PresetsList.SelectedItem is string selected)
            GoalInput.Text = selected;
    }
}