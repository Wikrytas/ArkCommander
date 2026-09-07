using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ArkCommander.Services;

public class MouseHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_XBUTTONDOWN = 0x020B;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelMouseProc? _proc;
    private readonly Dictionary<int, Action> _callbacks = new();

    public void Register(string button, Action callback)
    {
        int key = button.ToUpperInvariant() switch
        {
            "MOUSE1" => WM_LBUTTONDOWN,
            "MOUSE2" => WM_RBUTTONDOWN,
            "MOUSE3" => WM_MBUTTONDOWN,
            "MOUSE4" => WM_XBUTTONDOWN | (1 << 24),
            "MOUSE5" => WM_XBUTTONDOWN | (2 << 24),
            _ => 0
        };

        if (key == 0) return;

        _callbacks[key] = callback;
        Install();
    }

    private void Install()
    {
        if (_hookId != IntPtr.Zero) return;

        _proc = HookCallback;
        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc,
            GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;

            if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN ||
                msg == WM_MBUTTONDOWN || msg == WM_XBUTTONDOWN)
            {
                int key = msg;

                if (msg == WM_XBUTTONDOWN)
                {
                    var hs = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    int xb = (int)((hs.mouseData >> 16) & 0xFFFF);
                    key = msg | (xb << 24);
                }

                if (_callbacks.TryGetValue(key, out var cb))
                    cb();
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}
