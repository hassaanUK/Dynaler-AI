using System.Runtime.InteropServices;
using System.Text;

namespace DynalerAI.Core;

public static class Executor
{
    // ── P/Invoke declarations ─────────────────────────────────────────────────
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, int dx, int dy, uint data, int extra);
    [DllImport("user32.dll")] static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] static extern short VkKeyScan(char ch);

    const uint MOUSEEVENTF_LEFTDOWN  = 0x02;
    const uint MOUSEEVENTF_LEFTUP    = 0x04;
    const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
    const uint MOUSEEVENTF_RIGHTUP   = 0x10;

    // ── INPUT structs for SendInput ───────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public uint type; public INPUTUNION u; }

    [StructLayout(LayoutKind.Explicit)]
    struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public nint dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags; public uint time; public nint dwExtraInfo; }

    const uint INPUT_KEYBOARD    = 1;
    const uint KEYEVENTF_KEYUP   = 0x0002;
    const uint KEYEVENTF_UNICODE = 0x0004;

    // ── Public entry point ────────────────────────────────────────────────────
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
                TypeText(text);
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
            TypeText(app);
            await Task.Delay(500, token);
            PressVk(0x0D); // Enter
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
            PressVk(0x2C); // VK_SNAPSHOT (Print Screen)
        }

        await Task.Delay(delayMs, token);
    }

    // ── Keyboard helpers ──────────────────────────────────────────────────────

    /// <summary>Type arbitrary Unicode text using SendInput (no WinForms needed).</summary>
    private static void TypeText(string text)
    {
        var inputs = new List<INPUT>();
        foreach (char c in text)
        {
            inputs.Add(MakeUnicodeKey(c, false));
            inputs.Add(MakeUnicodeKey(c, true));
        }
        if (inputs.Count > 0)
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    private static INPUT MakeUnicodeKey(char c, bool keyUp) => new INPUT
    {
        type = INPUT_KEYBOARD,
        u = new INPUTUNION
        {
            ki = new KEYBDINPUT
            {
                wVk   = 0,
                wScan = c,
                dwFlags = KEYEVENTF_UNICODE | (keyUp ? KEYEVENTF_KEYUP : 0u),
            }
        }
    };

    private static void PressVk(ushort vk)
    {
        var inputs = new[]
        {
            new INPUT { type = INPUT_KEYBOARD, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk } } },
            new INPUT { type = INPUT_KEYBOARD, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP } } },
        };
        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void PressVkWithMod(ushort mod, ushort vk)
    {
        var inputs = new[]
        {
            new INPUT { type = INPUT_KEYBOARD, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = mod } } },
            new INPUT { type = INPUT_KEYBOARD, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk } } },
            new INPUT { type = INPUT_KEYBOARD, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP } } },
            new INPUT { type = INPUT_KEYBOARD, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = mod, dwFlags = KEYEVENTF_KEYUP } } },
        };
        SendInput(4, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void PressWindowsKey()
    {
        PressVk(0x5B); // VK_LWIN
    }

    private static void PressKey(string key)
    {
        var lower = key.Trim().ToLower();

        if (lower.StartsWith("ctrl+"))  { PressVkWithMod(0x11, CharOrNamedVk(key[5..])); return; }
        if (lower.StartsWith("alt+"))   { PressVkWithMod(0x12, CharOrNamedVk(key[4..])); return; }
        if (lower.StartsWith("shift+")) { PressVkWithMod(0x10, CharOrNamedVk(key[6..])); return; }

        PressVk(NamedVk(lower));
    }

    private static ushort CharOrNamedVk(string s)
    {
        if (s.Length == 1) return (ushort)(VkKeyScan(s[0]) & 0xFF);
        return NamedVk(s.Trim().ToLower());
    }

    private static ushort NamedVk(string name) => name switch
    {
        "enter" or "return"   => 0x0D,
        "tab"                 => 0x09,
        "escape" or "esc"     => 0x1B,
        "space"               => 0x20,
        "backspace"           => 0x08,
        "delete"              => 0x2E,
        "printscreen"         => 0x2C,
        "home"                => 0x24,
        "end"                 => 0x23,
        "pageup"              => 0x21,
        "pagedown"            => 0x22,
        "up"                  => 0x26,
        "down"                => 0x28,
        "left"                => 0x25,
        "right"               => 0x27,
        "f1"                  => 0x70,
        "f2"                  => 0x71,
        "f3"                  => 0x72,
        "f4"                  => 0x73,
        "f5"                  => 0x74,
        "f6"                  => 0x75,
        "f7"                  => 0x76,
        "f8"                  => 0x77,
        "f9"                  => 0x78,
        "f10"                 => 0x79,
        "f11"                 => 0x7A,
        "f12"                 => 0x7B,
        _                     => name.Length == 1 ? (ushort)(VkKeyScan(name[0]) & 0xFF) : (ushort)0,
    };

    // ── Mouse helpers ─────────────────────────────────────────────────────────
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

    // ── Text extraction helpers ───────────────────────────────────────────────
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
