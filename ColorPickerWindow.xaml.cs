using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace ArkCommander;

public partial class ColorPickerWindow : Window
{
    public string ResultHex { get; private set; } = "#FF000000";

    private bool _init;

    public ColorPickerWindow(string initial)
    {
        InitializeComponent();

        byte a = 255, r = 232, g = 236, b = 244;

        try
        {
            var c = (Color)ColorConverter.ConvertFromString(initial)!;
            a = c.A; r = c.R; g = c.G; b = c.B;
        }
        catch { }

        _init = true;
        ASlider.Value = a;
        RSlider.Value = r;
        GSlider.Value = g;
        BSlider.Value = b;
        _init = false;

        Update();
    }

    public static bool TryPick(Window owner, string initial, out string hex)
    {
        var win = new ColorPickerWindow(initial) { Owner = owner };

        if (win.ShowDialog() == true)
        {
            hex = win.ResultHex;
            return true;
        }

        hex = initial;
        return false;
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Update();
    }

    private void Update()
    {
        if (_init)
            return;

        byte a = (byte)ASlider.Value;
        byte r = (byte)RSlider.Value;
        byte g = (byte)GSlider.Value;
        byte b = (byte)BSlider.Value;

        ALabel.Text = a.ToString();
        RLabel.Text = r.ToString();
        GLabel.Text = g.ToString();
        BLabel.Text = b.ToString();

        var c = Color.FromArgb(a, r, g, b);

        Preview.Background = new SolidColorBrush(c);
        HexBox.Text = c.ToString();
        ResultHex = c.ToString();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
