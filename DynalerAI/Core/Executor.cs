using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DynalerAI.Core;

public static class Executor
{
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, int dx, int dy, uint data, int extra);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, int extra);

    const uint MOUSEEVENTF_LEFTDOWN  = 0x02;
    const uint MOUSEEVENTF_LEFTUP    = 0x04;
    const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
    const uint MOUSEEVENTF_RIGHTUP   = 0x10;
    const uint KEYEVENTF_KEYUP       = 0x02;

    public static async Task ExecuteStepAsync(string step, int delayMs, CancellationToken token)
    {
        var lower = step.ToLowerInvariant();

        if (lower.Contains("double-click") || lower.Contains("double click"))
        {
            await Task.Delay(300, token);
            Click(); await Task.Delay(80, token); Click();
        }
        else if (lower.Contains("right-click") || lower.Contains("right click"))
        {
            await Task.Delay(300, token);
            RightClick();
        }
        else if (lower.Contains("click"))
        {
            await Task.Delay(300, token);
            Click();
        }
        else if (lower.Contains("type ") || lower.Contains("write "))
        {
            var text = ExtractQuoted(step) ?? ExtractAfterKeyword(step, "type", "write");
            if (!string.IsNullOrEmpty(text))
            {
                await Task.Delay(200, token);
                // Escape SendKeys special characters: + ^ % ~ { } ( ) [ ]
                var escaped = EscapeSendKeys(text);
                SendKeys.SendWait(escaped);
            }
        }
        else if (lower.Contains("press "))
        {
            var key = ExtractAfterKeyword(step, "press");
            await Task.Delay(200, token);
            PressKey(key);
        }
        else if (lower.Contains("open "))
        {
            var app = ExtractAfterKeyword(step, "open");
            await Task.Delay(200, token);
            PressWindowsKey();
            await Task.Delay(800, token);
            SendKeys.SendWait(EscapeSendKeys(app));
            await Task.Delay(500, token);
            SendKeys.SendWait("{ENTER}");
        }
        else if (lower.Contains("scroll down"))
        {
            mouse_event(0x0800, 0, 0, unchecked((uint)-120), 0);
        }
        else if (lower.Contains("scroll up"))
        {
            mouse_event(0x0800, 0, 0, 120, 0);
        }
        else if (lower.Contains("screenshot"))
        {
            PressKey("PrintScreen");
        }

        await Task.Delay(delayMs, token);
    }

    // Escape characters that SendKeys treats as special: + ^ % ~ { } ( ) [ ]
    private static string EscapeSendKeys(string text)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in text)
        {
            if (c is '+' or '^' or '%' or '~' or '{' or '}' or '(' or ')' or '[' or ']')
                sb.Append('{').Append(c).Append('}');
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    private static void Click()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP,   0, 0, 0, 0);
    }

    private static void RightClick()
    {
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_RIGHTUP,   0, 0, 0, 0);
    }

    private static void PressWindowsKey()
    {
        keybd_event(0x5B, 0, 0, 0);
        keybd_event(0x5B, 0, KEYEVENTF_KEYUP, 0);
    }

    private static void PressKey(string key)
    {
        var lower = key.Trim().ToLower();

        // Handle Ctrl+X, Alt+X combos
        if (lower.StartsWith("ctrl+"))
        {
            SendKeys.SendWait($"^{EscapeSendKeys(key[5..].ToLower())}");
            return;
        }
        if (lower.StartsWith("alt+"))
        {
            SendKeys.SendWait($"%{EscapeSendKeys(key[4..].ToLower())}");
            return;
        }
        if (lower.StartsWith("shift+"))
        {
            SendKeys.SendWait($"+{EscapeSendKeys(key[6..].ToLower())}");
            return;
        }

        // Named keys
        var sendKey = lower switch
        {
            "enter" or "return"   => "{ENTER}",
            "tab"                 => "{TAB}",
            "escape" or "esc"     => "{ESC}",
            "space"               => " ",
            "backspace"           => "{BACKSPACE}",
            "delete"              => "{DELETE}",
            "printscreen"         => "{PRTSC}",
            "home"                => "{HOME}",
            "end"                 => "{END}",
            "pageup"              => "{PGUP}",
            "pagedown"            => "{PGDN}",
            "up"                  => "{UP}",
            "down"                => "{DOWN}",
            "left"                => "{LEFT}",
            "right"               => "{RIGHT}",
            "f1"                  => "{F1}",
            "f2"                  => "{F2}",
            "f3"                  => "{F3}",
            "f4"                  => "{F4}",
            "f5"                  => "{F5}",
            "f6"                  => "{F6}",
            "f7"                  => "{F7}",
            "f8"                  => "{F8}",
            "f9"                  => "{F9}",
            "f10"                 => "{F10}",
            "f11"                 => "{F11}",
            "f12"                 => "{F12}",
            _                     => key.Length == 1 ? EscapeSendKeys(key) : ""
        };

        if (!string.IsNullOrEmpty(sendKey))
            SendKeys.SendWait(sendKey);
    }

    private static string? ExtractQuoted(string text)
    {
        var start = text.IndexOf('"');
        var end   = text.LastIndexOf('"');
        if (start >= 0 && end > start) return text[(start + 1)..end];
        start = text.IndexOf('\'');
        end   = text.LastIndexOf('\'');
        if (start >= 0 && end > start) return text[(start + 1)..end];
        return null;
    }

    private static string ExtractAfterKeyword(string text, params string[] keywords)
    {
        var lower = text.ToLower();
        foreach (var kw in keywords)
        {
            var idx = lower.IndexOf(kw + " ");
            if (idx >= 0) return text[(idx + kw.Length + 1)..].Trim().Trim('"', '\'');
        }
        return text;
    }
}
