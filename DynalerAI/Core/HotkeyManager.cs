using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
namespace DynalerAI.Core;
public class HotkeyManager {
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
    const int ID = 9001;
    const uint CTRL = 0x0002, SHIFT = 0x0004, WM_HOTKEY = 0x0312;
    readonly Window _win; readonly Action _cb;
    public HotkeyManager(Window win, Action cb) {
        _win = win; _cb = cb;
        win.Loaded += (s,e) => { var src = HwndSource.FromHwnd(new WindowInteropHelper(win).Handle); src?.AddHook(Hook); };
        win.Closed += (s,e) => Unregister();
    }
    public void Register(Key key, ModifierKeys mods) {
        var h = new WindowInteropHelper(_win).Handle; if (h == IntPtr.Zero) return;
        uint m = 0; if (mods.HasFlag(ModifierKeys.Control)) m |= CTRL; if (mods.HasFlag(ModifierKeys.Shift)) m |= SHIFT;
        RegisterHotKey(h, ID, m, (uint)KeyInterop.VirtualKeyFromKey(key));
    }
    public void Unregister() { var h = new WindowInteropHelper(_win).Handle; if (h != IntPtr.Zero) UnregisterHotKey(h, ID); }
    IntPtr Hook(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp, ref bool handled) { if (msg == WM_HOTKEY && wp.ToInt32() == ID) { _cb(); handled = true; } return IntPtr.Zero; }
}
