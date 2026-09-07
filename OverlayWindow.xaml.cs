using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ArkCommander.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using BrushConverter = System.Windows.Media.BrushConverter;
using FontFamily = System.Windows.Media.FontFamily;
using Orientation = System.Windows.Controls.Orientation;

namespace ArkCommander;

public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private bool _drag;
    private TextBlock? _badge;

    public void SetListening(bool on)
    {
        if (_badge != null)
            _badge.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
    }

    public OverlayWindow(List<CommandItem> commands, OverlaySettings s)
    {
        InitializeComponent();

        Opacity = s.Opacity;
        FontFamily = new FontFamily(s.FontFamily);

        RootBorder.Background = Hex(s.BackgroundColor);

        var panel = new StackPanel();

        _badge = new TextBlock
        {
            Text = "🎙 слушаю…",
            Foreground = (Brush)new BrushConverter().ConvertFromString("#33DD66")!,
            FontWeight = FontWeights.SemiBold,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(6, 2, 6, 2)
        };
        panel.Children.Add(_badge);

        foreach (var cmd in commands)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(6, 1, 6, 1)
            };

            if (s.ShowHotkeys)
            {
                row.Children.Add(new TextBlock
                {
                    Text = cmd.Hotkey,
                    Foreground = Hex(s.HotkeyColor),
                    FontSize = Math.Max(8, s.FontSize - 1),
                    Width = 72,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            row.Children.Add(new TextBlock
            {
                Text = cmd.Short,
                Foreground = Hex(s.TextColor),
                FontSize = s.FontSize,
                VerticalAlignment = VerticalAlignment.Center
            });

            panel.Children.Add(row);
        }

        RootBorder.Child = panel;

        Loaded += (_, _) => Place(s);
    }

    private void Place(OverlaySettings s)
    {
        var work = SystemParameters.WorkArea;
        int m = s.Margin;

        switch (s.Position)
        {
            case "TopLeft":
                Left = work.Left + m; Top = work.Top + m; break;
            case "TopRight":
                Left = work.Right - ActualWidth - m; Top = work.Top + m; break;
            case "BottomLeft":
                Left = work.Left + m; Top = work.Bottom - ActualHeight - m; break;
            case "BottomRight":
                Left = work.Right - ActualWidth - m; Top = work.Bottom - ActualHeight - m; break;
            case "BottomCenter":
                Left = work.Left + (work.Width - ActualWidth) / 2; Top = work.Bottom - ActualHeight - m; break;
            case "Custom":
                Left = work.Left + work.Width * s.XPercent; Top = work.Top + work.Height * s.YPercent; break;
            default:
                Left = work.Left + (work.Width - ActualWidth) / 2; Top = work.Top + m; break;
        }
    }

    public void EnableDrag(bool on)
    {
        _drag = on;

        IsHitTestVisible = on;
        Focusable = on;
        ShowActivated = on;

        var hwnd = new WindowInteropHelper(this).Handle;

        int style = GetWindowLong(hwnd, GWL_EXSTYLE);

        if (on) style &= ~WS_EX_TRANSPARENT;
        else style |= WS_EX_TRANSPARENT;

        SetWindowLong(hwnd, GWL_EXSTYLE, style);

        RootBorder.BorderBrush = on
            ? (Brush)new BrushConverter().ConvertFromString("#3FA7FF")!
            : (Brush)FindResource("BorderBrush");
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (_drag)
            DragMove();
    }

    private static Brush Hex(string hex)
    {
        try { return (Brush)new BrushConverter().ConvertFromString(hex)!; }
        catch { return Brushes.White; }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;

        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        style |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, GWL_EXSTYLE, style);
    }
}

