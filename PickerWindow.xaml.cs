using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ArkCommander.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using BrushConverter = System.Windows.Media.BrushConverter;
using Image = System.Windows.Controls.Image;
using Path = System.IO.Path;
using Cursors = System.Windows.Input.Cursors;
using Orientation = System.Windows.Controls.Orientation;

namespace ArkCommander;

public partial class PickerWindow : Window
{
    public string SelectedId { get; private set; } = "";
    public string SelectedName { get; private set; } = "";
    public int Quantity { get; private set; } = 1;
    public bool UsedTyped { get; private set; }

    private readonly List<PickerEntry> _all;
    private readonly bool _allowTyped;

    private Border? _selectedRow;
    private PickerEntry? _selectedEntry;

    private static readonly BitmapSource? Coin = LoadCoin();

    private static BitmapSource? LoadCoin()
    {
        try
        {
            var p = Path.Combine(AppContext.BaseDirectory, "icons", "arkcoin.png");

            if (!File.Exists(p))
                return null;

            using var raw = new System.Drawing.Bitmap(p);
            using var clean = Services.IconService.CleanAndCrop(raw);

            var hb = clean.GetHbitmap();

            var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hb,
                IntPtr.Zero,
                new Int32Rect(),
                BitmapSizeOptions.FromEmptyOptions());

            bs.Freeze();

            return bs;
        }
        catch
        {
            return null;
        }
    }

    public PickerWindow(string title, List<PickerEntry> entries, bool showQty, bool allowTyped = false)
    {
        InitializeComponent();

        TitleText.Text = title;
        _all = entries;
        _allowTyped = allowTyped;

        QtyRow.Visibility = showQty ? Visibility.Visible : Visibility.Collapsed;

        BuildRows("");
    }

    private void BuildRows(string filter)
    {
        RowsPanel.Children.Clear();
        _selectedRow = null;
        _selectedEntry = null;

        var list = string.IsNullOrWhiteSpace(filter)
            ? _all
            : _all.Where(e => (e.Name + e.Id + e.Details)
                .Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var e in list)
        {
            var row = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                Tag = e,
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new Image
            {
                Width = 34,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0)
            };

            if (e.HasIcon)
            {
                try
                {
                    var b = new BitmapImage();
                    b.BeginInit();
                    b.UriSource = new Uri(e.IconPath);
                    b.CacheOption = BitmapCacheOption.OnLoad;
                    b.EndInit();
                    b.Freeze();

                    icon.Source = b;
                }
                catch { }
            }

            grid.Children.Add(icon);
            Grid.SetColumn(icon, 0);

            var texts = new StackPanel();

            texts.Children.Add(new TextBlock
            {
                Text = e.Name,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            if (!string.IsNullOrWhiteSpace(e.Details))
            {
                texts.Children.Add(new TextBlock
                {
                    Text = e.Details,
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#8A93A6")!,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            grid.Children.Add(texts);
            Grid.SetColumn(texts, 1);

            var price = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            if (Coin != null)
            {
                price.Children.Add(new Image
                {
                    Width = 24,
                    Height = 24,
                    Source = Coin,
                    Margin = new Thickness(0, 0, 5, 0)
                });
            }
            else
            {
                price.Children.Add(MakeCoinVisual(24));
                price.Children.Add(new FrameworkElement { Width = 5 });
            }

            price.Children.Add(new TextBlock
            {
                Text = e.PriceText,
                Foreground = (Brush)new BrushConverter().ConvertFromString("#3FA7FF")!,
                FontWeight = FontWeights.SemiBold
            });

            grid.Children.Add(price);
            Grid.SetColumn(price, 2);

            row.Child = grid;
            row.MouseLeftButtonUp += Row_Click;

            RowsPanel.Children.Add(row);
        }
    }

    private static UIElement MakeCoinVisual(double size)
    {
        var grid = new Grid
        {
            Width = size,
            Height = size,
            Margin = new Thickness(0, 0, 5, 0)
        };

        var circle = new Ellipse
        {
            Fill = (Brush)new BrushConverter().ConvertFromString("#D4A017")!,
            Stroke = (Brush)new BrushConverter().ConvertFromString("#8A6A10")!,
            StrokeThickness = 1
        };

        var letter = new TextBlock
        {
            Text = "A",
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            FontSize = size * 0.6
        };

        grid.Children.Add(circle);
        grid.Children.Add(letter);

        return grid;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void Row_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border row)
            return;

        if (_selectedRow != null)
            _selectedRow.Background = Brushes.Transparent;

        _selectedRow = row;
        _selectedEntry = row.Tag as PickerEntry;

        row.Background = (Brush)new BrushConverter().ConvertFromString("#1A2130")!;
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        BuildRows(SearchBox.Text);
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEntry != null)
        {
            SelectedId = _selectedEntry.Id;
            SelectedName = _selectedEntry.Name;
        }
        else if (_allowTyped && !string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            UsedTyped = true;
            SelectedId = SearchBox.Text.Trim();
            SelectedName = SearchBox.Text.Trim();
        }
        else
        {
            return;
        }

        int.TryParse(QtyBox.Text, out int q);
        Quantity = q < 1 ? 1 : q;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}



