using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ArkCommander.Models;
using ArkCommander.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;

namespace ArkCommander;

public enum PickerMode { Buy, Kit, Sale }

public partial class MainWindow : Window
{
    private AppConfig _config = new();
    private readonly HotkeyService _hotkeys = new();
    private readonly MouseHook _mouseHook = new();
    private readonly System.Windows.Threading.DispatcherTimer _gameTimer = new();
    private OverlayWindow? _overlay;
    private System.Windows.Forms.NotifyIcon? _tray;
    private bool _reallyClose;

    private static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "config.json");

    public MainWindow()
    {
        InitializeComponent();
        _config = LoadConfig();

        Loaded += (_, _) =>
        {
            BuildCommands();
            BuildTracks();
            FillSettings();
            InitTray();
            RegisterHotkeys();

            if (_config.Settings.OverlayOnStartup)
                ShowOverlay();

            _gameTimer.Interval = TimeSpan.FromSeconds(2);
            _gameTimer.Tick += (_, _) => UpdateGameStatus();
            _gameTimer.Start();
            UpdateGameStatus();
            SetStatus("готов");
        };
    }

    private AppConfig LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
                return System.Text.Json.JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath)) ?? new AppConfig();
        }
        catch { }
        return new AppConfig();
    }

    private void SaveConfig()
    {
        try
        {
            File.WriteAllText(ConfigPath,
                System.Text.Json.JsonSerializer.Serialize(_config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private void SetStatus(string s) => StatusText.Text = $"[{DateTime.Now:HH:mm:ss}] {s}";

    private void TitleBar_MouseLeftButtonDown(object s, MouseButtonEventArgs e) { if (e.ClickCount == 1) DragMove(); }
    private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object s, RoutedEventArgs e) { _reallyClose = true; Close(); }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_reallyClose && _config.Settings.CloseToTray)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            try { _tray?.Dispose(); } catch { }
        }
        base.OnClosing(e);
    }

    private void ShowPage(string p)
    {
        PageCommands.Visibility = p == "commands" ? Visibility.Visible : Visibility.Collapsed;
        PageShop.Visibility = p == "shop" ? Visibility.Visible : Visibility.Collapsed;
        PageTracks.Visibility = p == "tracks" ? Visibility.Visible : Visibility.Collapsed;
        PageAtlas.Visibility = p == "atlas" ? Visibility.Visible : Visibility.Collapsed;
        PageSettings.Visibility = p == "settings" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SideCommands_Click(object s, RoutedEventArgs e) => ShowPage("commands");
    private void SideShop_Click(object s, RoutedEventArgs e) => ShowPage("shop");
    private void SideTrack_Click(object s, RoutedEventArgs e) => ShowPage("tracks");
    private void SideAtlas_Click(object s, RoutedEventArgs e) => ShowPage("atlas");
    private void SideSettings_Click(object s, RoutedEventArgs e) { FillSettings(); ShowPage("settings"); }

    private void BuildCommands()
    {
        CommandsPanel.Children.Clear();
        foreach (var cmd in _config.Commands.Where(c => c.ShowOnPanel))
        {
            var card = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#141C2B"),
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#26334A"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(8),
                Padding = new Thickness(14),
                Width = 260
            };
            var sp = new StackPanel();
            var head = new StackPanel { Orientation = Orientation.Horizontal };
            head.Children.Add(new TextBlock { Text = cmd.Name, FontSize = 15, FontWeight = FontWeights.SemiBold });
            head.Children.Add(new TextBlock { Text = cmd.Hotkey, Foreground = (Brush)new BrushConverter().ConvertFromString("#3FA7FF"), Margin = new Thickness(10, 0, 0, 0) });
            sp.Children.Add(head);
            sp.Children.Add(new TextBlock { Text = cmd.Command, Foreground = (Brush)new BrushConverter().ConvertFromString("#8A93A6"), Margin = new Thickness(0, 4, 0, 10) });
            var b = new Button { Content = "Отправить" };
            var cc = cmd.Command;
            b.Click += (_, _) => _ = SendCommandAsync(cc);
            sp.Children.Add(b);
            card.Child = sp;
            CommandsPanel.Children.Add(card);
        }
    }

    private void BuildTracks()
    {
        TracksPanel.Children.Clear();
        foreach (var t in _config.TrackItems)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            row.Children.Add(new TextBlock { Text = "🎯 " + t.Name, Width = 240, VerticalAlignment = VerticalAlignment.Center });
            var go = new Button { Content = "Трекнуть", Margin = new Thickness(0, 0, 6, 0) };
            var name = t.Name;
            go.Click += (_, _) => _ = SendCommandAsync(TemplateHelper.Build(_config.Templates.TrackCommandTemplate, new Dictionary<string, string> { ["name"] = name, ["id"] = name }));
            row.Children.Add(go);
            TracksPanel.Children.Add(row);
        }
    }

    private void AddTrack_Click(object s, RoutedEventArgs e)
    {
        var n = TrackNameBox.Text.Trim();
        if (n.Length == 0) return;
        _config.TrackItems.Add(new TrackItem { Name = n });
        SaveConfig();
        BuildTracks();
        TrackNameBox.Text = "";
    }

    private void ShopBuy_Click(object s, RoutedEventArgs e) => OpenPicker(PickerMode.Buy);
    private void ShopKit_Click(object s, RoutedEventArgs e) => OpenPicker(PickerMode.Kit);
    private void ShopSale_Click(object s, RoutedEventArgs e) => OpenPicker(PickerMode.Sale);

    private void OpenPicker(PickerMode mode)
    {
        List<PickerEntry> entries = new();
        string title = ""; bool qty = false;

        if (mode == PickerMode.Buy)
        {
            title = "Покупка"; qty = true;
            entries = _config.BuyItems.Select(b => new PickerEntry { Id = b.Id, Name = b.Name, Details = b.Cat + " · макс " + b.Qty, PriceText = b.Price.ToString(), IconPath = IconService.IconPathFor(b) ?? "" }).ToList();
        }
        else if (mode == PickerMode.Kit)
        {
            title = "Киты";
            entries = _config.KitItems.Select(k => new PickerEntry { Id = k.Id, Name = k.Name, Details = k.Qty, PriceText = k.Price.ToString(), IconPath = IconService.KitIconPathFor(k) ?? "" }).ToList();
        }
        else
        {
            title = "Продажа";
            entries = _config.SaleItems.Select(x => new PickerEntry { Id = x.Id, Name = x.Name, Details = x.Qty, PriceText = x.Price.ToString(), IconPath = IconService.SaleIconPathFor(x) ?? "" }).ToList();
        }

        var w = new PickerWindow(title, entries, qty) { Owner = this };
        if (w.ShowDialog() != true) return;

        string tpl = mode == PickerMode.Buy ? _config.Templates.BuyCommandTemplate
                   : mode == PickerMode.Kit ? _config.Templates.KitCommandTemplate
                   : _config.Templates.SaleCommandTemplate;

        var cmd = TemplateHelper.Build(tpl, new Dictionary<string, string>
        {
            ["id"] = w.SelectedId,
            ["name"] = w.SelectedName,
            ["qty"] = w.Quantity.ToString()
        });
        _ = SendCommandAsync(cmd);
    }

    private void FillSettings()
    {
        ChatKeyBox.Text = _config.Settings.ChatKey;
        OverlayOnStartupCheck.IsChecked = _config.Settings.OverlayOnStartup;
        OverlayTopmostCheck.IsChecked = _config.Settings.OverlayTopmost;
        CloseToTrayCheck.IsChecked = _config.Settings.CloseToTray;
    }

    private void SaveSettings_Click(object s, RoutedEventArgs e)
    {
        if (ChatKeyBox.Text.Trim().Length > 0) _config.Settings.ChatKey = ChatKeyBox.Text.Trim();
        _config.Settings.OverlayOnStartup = OverlayOnStartupCheck.IsChecked == true;
        _config.Settings.OverlayTopmost = OverlayTopmostCheck.IsChecked == true;
        _config.Settings.CloseToTray = CloseToTrayCheck.IsChecked == true;
        SaveConfig();

        if (_overlay != null) _overlay.Topmost = _config.Settings.OverlayTopmost;

        SetStatus("настройки сохранены");
    }

    private void ShowOverlay()
    {
        if (_overlay == null)
        {
            _overlay = new OverlayWindow(_config.Commands, _config.Settings.Overlay);
            _overlay.Topmost = _config.Settings.OverlayTopmost;
            _overlay.Show();
        }
    }

    private void ToggleOverlay_Click(object s, RoutedEventArgs e)
    {
        if (_overlay == null) ShowOverlay();
        else { _overlay.Close(); _overlay = null; }
    }

    private void InitTray()
    {
        _tray = new System.Windows.Forms.NotifyIcon();
        try
        {
            var ip = Path.Combine(AppContext.BaseDirectory, "icon.ico");
            _tray.Icon = File.Exists(ip) ? new System.Drawing.Icon(ip) : System.Drawing.SystemIcons.Application;
        }
        catch { _tray.Icon = System.Drawing.SystemIcons.Application; }
        _tray.Text = "ARK Commander";
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Показать", null, (_, _) => Dispatcher.BeginInvoke(new Action(() => { Show(); Activate(); })));
        menu.Items.Add("Выход", null, (_, _) => Dispatcher.BeginInvoke(new Action(() => { _reallyClose = true; Close(); })));
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => Dispatcher.BeginInvoke(new Action(() => { Show(); Activate(); }));
        _tray.Visible = true;
    }

    private void RegisterHotkeys()
    {
        foreach (var cmd in _config.Commands.Where(c => !string.IsNullOrWhiteSpace(c.Hotkey)))
        {
            var cc = cmd.Command;
            if (cmd.Hotkey.StartsWith("MOUSE", StringComparison.OrdinalIgnoreCase))
                _mouseHook.Register(cmd.Hotkey, () => Dispatcher.BeginInvoke(new Action(() => _ = SendCommandAsync(cc))));
            else
                _hotkeys.Register(cmd.Hotkey, cmd.Mod, () => Dispatcher.BeginInvoke(new Action(() => _ = SendCommandAsync(cc))));
        }
    }

    private static readonly string[] GameProc = { "ArkAscended", "ArkSurvivalAscended", "ShooterGame" };

    private void UpdateGameStatus()
    {
        bool run = GameProc.Any(n => Process.GetProcessesByName(n).Length > 0);
        GameDot.Fill = (Brush)new BrushConverter().ConvertFromString(run ? "#22C55E" : "#6B7280");
        GameStatusText.Text = run ? "игра запущена" : "игра не запущена";
    }

    private static IntPtr FindGameWindow()
    {
        foreach (var n in GameProc)
        {
            var p = Process.GetProcessesByName(n).FirstOrDefault();
            if (p != null) return p.MainWindowHandle;
        }
        return IntPtr.Zero;
    }

    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint proc);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint a, uint b, bool attach);
    [DllImport("user32.dll")] private static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] private static extern ushort VkKeyScanW(char ch);

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT { [FieldOffset(0)] public uint type; [FieldOffset(8)] public KEYBDINPUT ki; }
    [DllImport("user32.dll")] private static extern uint SendInput(uint n, INPUT[] p, int cb);

    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static void TypeText(string s)
    {
        foreach (var ch in s)
        {
            var down = new INPUT { type = 1, ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE } };
            var up = new INPUT { type = 1, ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } };
            SendInput(2, new[] { down, up }, Marshal.SizeOf<INPUT>());
        }
    }

    private static void PressVk(byte vk)
    {
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, 2, UIntPtr.Zero);
    }

    private async System.Threading.Tasks.Task SendCommandAsync(string command)
    {
        try
        {
            var hw = FindGameWindow();
            if (hw == IntPtr.Zero) { SetStatus("игра не найдена: " + command); return; }

            await System.Threading.Tasks.Task.Delay(50);
            uint fgProc;
            var fgThread = GetWindowThreadProcessId(GetForegroundWindow(), out fgProc);
            var curThread = GetCurrentThreadId();
            AttachThreadInput(curThread, fgThread, true);
            SetForegroundWindow(hw);
            AttachThreadInput(curThread, fgThread, false);

            await System.Threading.Tasks.Task.Delay(120);
            byte chatVk = _config.Settings.ChatKey == "Enter" ? (byte)0x0D : (byte)(VkKeyScanW(_config.Settings.ChatKey[0]) & 0xFF);
            PressVk(chatVk);
            await System.Threading.Tasks.Task.Delay(150);
            TypeText(command);
            await System.Threading.Tasks.Task.Delay(120);
            PressVk(0x0D);

            SetStatus("отправлено: " + command);
        }
        catch (Exception ex) { SetStatus("ошибка отправки: " + ex.Message); }
    }
}
