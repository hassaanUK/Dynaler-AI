using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DynalerAI.Core;

public class HotkeyManager
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    const int  HotkeyId   = 9001;
    const uint MOD_CONTROL = 0x0002;
    const uint MOD_SHIFT   = 0x0004;
    const uint WM_HOTKEY   = 0x0312;

    private readonly Window  _window;
    private readonly Action  _callback;
    private HwndSource?      _source;
    private Key              _registeredKey;
    private ModifierKeys     _registeredMods;
    private bool             _registered;

    public HotkeyManager(Window window, Action callback)
    {
        _window   = window;
        _callback = callback;
        // Hook after window is fully loaded so the HWND exists
        _window.Loaded += OnLoaded;
        _window.Closed += OnClosed;
    }

    private void OnLoaded(object s, RoutedEventArgs e)
    {
        var helper = new WindowInteropHelper(_window);
        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(HwndHook);

        // Re-register if Register() was called before window was loaded
        if (_registeredKey != Key.None)
            Register(_registeredKey, _registeredMods);
    }

    private void OnClosed(object? s, EventArgs e) => Unregister();

    public void Register(Key key, ModifierKeys modifiers)
    {
        // Store in case the window isn't loaded yet
        _registeredKey  = key;
        _registeredMods = modifiers;

        var helper = new WindowInteropHelper(_window);
        if (helper.Handle == IntPtr.Zero) return; // Will re-register in OnLoaded

        // Unregister previous before re-registering (avoids duplicate ID errors)
        if (_registered) UnregisterHotKey(helper.Handle, HotkeyId);

        uint mods = 0;
        if (modifiers.HasFlag(ModifierKeys.Control)) mods |= MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift))   mods |= MOD_SHIFT;

        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        _registered = RegisterHotKey(helper.Handle, HotkeyId, mods, vk);
    }

    public void Unregister()
    {
        var helper = new WindowInteropHelper(_window);
        if (helper.Handle != IntPtr.Zero && _registered)
        {
            UnregisterHotKey(helper.Handle, HotkeyId);
            _registered = false;
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            _callback();
            handled = true;
        }
        return IntPtr.Zero;
    }
}
