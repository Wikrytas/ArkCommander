using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ArkCommander.Models;

namespace ArkCommander.Services;

public class ArkWindowService
{
    private const int SW_SHOWNORMAL = 1;
    private const int SW_RESTORE = 9;

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint VK_MENU = 0x12;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public IntPtr FindGameHwnd(AppSettings settings)
    {
        Process? process = null;

        if (!string.IsNullOrWhiteSpace(settings.GameProcessName))
        {
            process = Process.GetProcessesByName(settings.GameProcessName)
                .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
        }

        if (process == null && !string.IsNullOrWhiteSpace(settings.GameWindowTitle))
        {
            process = Process.GetProcesses()
                .FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.MainWindowTitle) &&
                    p.MainWindowTitle.Contains(settings.GameWindowTitle, StringComparison.OrdinalIgnoreCase));
        }

        return process?.MainWindowHandle ?? IntPtr.Zero;
    }

    public void FocusHwnd(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return;

        if (IsIconic(hwnd))
            ShowWindow(hwnd, SW_RESTORE);
        else
            ShowWindow(hwnd, SW_SHOWNORMAL);

        SendAltTap();

        SetForegroundWindow(hwnd);

        Thread.Sleep(150);
    }

    public bool TryFocusGame(AppSettings settings)
    {
        IntPtr hwnd = FindGameHwnd(settings);

        if (hwnd == IntPtr.Zero)
            return false;

        FocusHwnd(hwnd);

        return true;
    }

    private static void SendAltTap()
    {
        var inputs = new INPUT[]
        {
            new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)VK_MENU,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            },
            new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)VK_MENU,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
