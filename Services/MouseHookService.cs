using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Application = System.Windows.Application;

namespace ArkCommander.Services;

public class MouseHookService : IDisposable
{
    private const int WM_INPUT = 0x00FF;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const uint RID_INPUT = 0x10000003;
    private const int RIM_TYPEMOUSE = 0;

    private const ushort RI_MOUSE_BUTTON3_DOWN = 0x0010;
    private const ushort RI_MOUSE_BUTTON4_DOWN = 0x0040;
    private const ushort RI_MOUSE_BUTTON5_DOWN = 0x0080;

    private static readonly string DebugLog =
        Path.Combine(AppContext.BaseDirectory, "mouse_debug.txt");

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(
        ref RAWINPUTDEVICE pRawInputDevice, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(
        IntPtr hRawInput, uint uiCommand, byte[]? pData, ref uint pcbSize, uint cbSizeHeader);

    private HwndSource? _source;
    private readonly Dictionary<string, Action> _actions = new();

    public Action<string>? OnDebug;

    public void Attach(Window window)
    {
        if (_source != null)
            return;

        IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();

        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);

        var device = new RAWINPUTDEVICE
        {
            UsagePage = 0x01,
            Usage = 0x02,
            Flags = RIDEV_INPUTSINK,
            Target = hwnd
        };

        bool ok = RegisterRawInputDevices(ref device, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());

        File.WriteAllText(DebugLog,
            $"[{DateTime.Now:HH:mm:ss}] attach ok={ok}\n");

        OnDebug?.Invoke(ok ? "raw input активен" : "raw input НЕ зарегистрирован");
    }

    public void Register(string button, Action callback)
    {
        _actions[button.ToUpperInvariant()] = callback;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_INPUT)
        {
            if (TryReadMouse(lParam, out ushort flags))
            {
                File.AppendAllText(DebugLog,
                    $"[{DateTime.Now:HH:mm:ss}] mouse flags={flags:X4}\n");

                string? button = MapButton(flags);

                if (button != null)
                {
                    var b = button;

                    Application.Current?.Dispatcher.BeginInvoke(new Action(() => OnDebug?.Invoke(b)));

                    if (_actions.TryGetValue(button, out var action))
                    {
                        Application.Current?.Dispatcher.BeginInvoke(action);
                    }
                }
            }
        }

        return IntPtr.Zero;
    }

    private static bool TryReadMouse(IntPtr hRawInput, out ushort flags)
    {
        flags = 0;

        uint size = 0;

        GetRawInputData(hRawInput, RID_INPUT, null, ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>());

        if (size == 0)
            return false;

        byte[] buffer = new byte[size];

        if (GetRawInputData(hRawInput, RID_INPUT, buffer, ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>()) == unchecked((uint)-1))
            return false;

        int type = BitConverter.ToInt32(buffer, 0);

        if (type != RIM_TYPEMOUSE)
            return false;

        flags = BitConverter.ToUInt16(buffer, 24 + 4);

        return true;
    }

    private static string? MapButton(ushort flags)
    {
        if ((flags & RI_MOUSE_BUTTON4_DOWN) != 0)
            return "MOUSE4";

        if ((flags & RI_MOUSE_BUTTON5_DOWN) != 0)
            return "MOUSE5";

        if ((flags & RI_MOUSE_BUTTON3_DOWN) != 0)
            return "MOUSE3";

        return null;
    }

    public void Dispose()
    {
        if (_source != null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }

        _actions.Clear();
    }
}
