using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ArkCommander.Helpers;

namespace ArkCommander.Services;

public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private HwndSource? _source;
    private int _nextId;
    private readonly Dictionary<int, Action> _actions = new();

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public void Attach(Window window)
    {
        var helper = new WindowInteropHelper(window);
        IntPtr hwnd = helper.EnsureHandle();

        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);
    }

    public bool Register(string key, string mod, Action callback)
    {
        if (_source == null)
            return false;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (!KeyMapper.TryGetVk(key, out uint vk))
            return false;

        int id = ++_nextId;
        uint modifiers = GetModifiers(mod) | MOD_NOREPEAT;

        if (!RegisterHotKey(_source.Handle, id, modifiers, vk))
            return false;

        _actions[id] = callback;
        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();

            if (_actions.TryGetValue(id, out var action))
            {
                action();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private static uint GetModifiers(string mod)
    {
        if (string.IsNullOrWhiteSpace(mod))
            return 0;

        uint result = 0;
        string m = mod.Trim();

        if (m.Contains("alt", StringComparison.OrdinalIgnoreCase))
            result |= MOD_ALT;

        if (m.Contains("ctrl", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("control", StringComparison.OrdinalIgnoreCase))
            result |= MOD_CONTROL;

        if (m.Contains("shift", StringComparison.OrdinalIgnoreCase))
            result |= MOD_SHIFT;

        if (m.Contains("win", StringComparison.OrdinalIgnoreCase))
            result |= MOD_WIN;

        return result;
    }

    public void Dispose()
    {
        if (_source != null)
        {
            foreach (int id in _actions.Keys)
            {
                UnregisterHotKey(_source.Handle, id);
            }

            _source.RemoveHook(WndProc);
            _source = null;
        }

        _actions.Clear();
    }
}