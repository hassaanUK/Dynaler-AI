using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace DynalerAI.Core;
public static class Executor {
    [DllImport("user32.dll")] static extern void mouse_event(uint f, int x, int y, uint d, int e);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte sc, uint f, int e);
    const uint LD=0x02,LU=0x04,RD=0x08,RU=0x10,KU=0x02;
    public static async Task ExecuteStepAsync(string step, CancellationToken token) {
        var lo = step.ToLowerInvariant();
        if (lo.Contains("open ")) {
            keybd_event(0x5B,0,0,0); keybd_event(0x5B,0,KU,0);
            await Task.Delay(800,token); SendKeys.SendWait(After(step,"open")); await Task.Delay(500,token); SendKeys.SendWait("{ENTER}");
        } else if (lo.Contains("type ")||lo.Contains("write ")) { await Task.Delay(200,token); SendKeys.SendWait(Quoted(step)??After(step,"type","write")); }
        else if (lo.Contains("press ")) { await Task.Delay(200,token); PressKey(After(step,"press")); }
        else if (lo.Contains("right-click")||lo.Contains("right click")) { mouse_event(RD,0,0,0,0); mouse_event(RU,0,0,0,0); }
        else if (lo.Contains("double-click")||lo.Contains("double click")) { Click(); await Task.Delay(80,token); Click(); }
        else if (lo.Contains("click")) { await Task.Delay(300,token); Click(); }
        else if (lo.Contains("scroll down")) mouse_event(0x0800,0,0,unchecked((uint)-120),0);
        else if (lo.Contains("scroll up")) mouse_event(0x0800,0,0,120,0);
        await Task.Delay(150,token);
    }
    static void Click() { mouse_event(LD,0,0,0,0); mouse_event(LU,0,0,0,0); }
    static void PressKey(string k) {
        if (k.ToLower().StartsWith("ctrl+")) { SendKeys.SendWait("^"+k[5..].ToLower()); return; }
        if (k.ToLower().StartsWith("alt+")) { SendKeys.SendWait("%"+k[4..].ToLower()); return; }
        var s = k.Trim().ToLower() switch { "enter" or "return" => "{ENTER}", "tab" => "{TAB}", "esc" or "escape" => "{ESC}", "backspace" => "{BACKSPACE}", "delete" => "{DELETE}", _ => k };
        SendKeys.SendWait(s);
    }
    static string? Quoted(string t) { var s=t.IndexOf('"''); var e=t.LastIndexOf('"''); return s>=0&&e>s?t[(s+1)..e]:null; }
    static string After(string t, params string[] kws) { var lo=t.ToLower(); foreach(var k in kws){var i=lo.IndexOf(k+" ");if(i>=0)return t[(i+k.Length+1)..].Trim().Trim('"'',''''');} return t; }
}
