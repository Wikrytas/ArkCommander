using System.IO;
using System.Net.Http;
using System.Text.Json;
using ArkCommander.Models;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ArkCommander.Services;

public class IconService
{
    private static readonly HttpClient Http = new();

    static IconService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ArkCommander/1.0");
        Http.Timeout = TimeSpan.FromSeconds(20);
    }

    public static string IconsDir => Path.Combine(AppContext.BaseDirectory, "icons");

    private static readonly Dictionary<string, string> WikiPages = new()
    {
        ["дерево"] = "Wood",
        ["грибная"] = "Fungal Wood",
        ["кристальная"] = "Crystalized Wood",
        ["камень"] = "Stone",
        ["волокно"] = "Fiber",
        ["солома"] = "Thatch",
        ["кремень"] = "Flint",
        ["кожа"] = "Hide",
        ["нефть"] = "Oil",
        ["паста"] = "Cementing Paste",
        ["шелк"] = "Silk",
        ["уголь"] = "Charcoal",
        ["хитин"] = "Chitin",
        ["порох"] = "Gunpowder",
        ["наркотик"] = "Narcotic",
        ["стимулятор"] = "Stimulant",
        ["полимер"] = "Polymer",
        ["кристалл"] = "Crystal",
        ["газ"] = "Congealed Gas Ball",
        ["мутагель"] = "Mutagel",
        ["мутаген"] = "Mutagen",
        ["элемент"] = "Element",
        ["обсидиан"] = "Obsidian",
        ["электроника"] = "Electronics",
        ["слиток"] = "Metal Ingot",
        ["зеленый"] = "Green Gem",
        ["синий"] = "Blue Gem",
        ["красный"] = "Red Gem",
        ["жемчуг"] = "Silica Pearls",
        ["черный"] = "Black Pearl",
        ["бензин"] = "Gasoline",
        ["молоко"] = "Wyvern Milk",
        ["яд"] = "Nameless Venom",
        ["страйдер"] = "Tek Strider",
        ["страйдер2"] = "Tek Strider",
        ["страйдер3"] = "Tek Strider",
    };

    private static readonly Dictionary<string, string> KitPages = new()
    {
        ["dino"] = "Pteranodon",
        ["tools"] = "Metal Hatchet",
        ["armor"] = "Hide Shirt",
        ["weapon"] = "Crossbow",
        ["ammo"] = "Stone Arrow",
        ["food"] = "Cooked Meat",
    };

    private static readonly Dictionary<string, string> SalePages = new()
    {
        ["праматерь"] = "Broodmother Lysrix",
        ["мегапитек"] = "Megapithecus",
        ["дракон"] = "Dragon",
        ["мантикора"] = "Manticore",
        ["роквелл"] = "Rockwell",
        ["король"] = "King Titan",
        ["виверна"] = "Crystal Wyvern",
        ["контроллер"] = "Overseer",
        ["динопитек"] = "Dinopithecus",
        ["роквелл2"] = "Rockwell Prime",
        ["фенрис"] = "Fenrir",
        ["моэдер"] = "Moeder",
        ["червь"] = "Deathworm",
        ["авиверна"] = "Wyvern",
        ["рекс"] = "Rex",
        ["бейла"] = "Beyla",
        ["хати"] = "Hati",
        ["сколл"] = "Skoll",
        ["стейнбьерн"] = "Steinbjorn",
        ["полимер"] = "Polymer",
        ["элемент"] = "Element",
    };

    public static string? SaleIconPathFor(SaleItem s)
    {
        var p = Path.Combine(IconsDir, "sale_" + SafeName(s.Id) + ".png");
        return File.Exists(p) ? p : null;
    }

    public async Task DownloadSaleAsync(List<SaleItem> items, Action<string> status)
    {
        try
        {
            Directory.CreateDirectory(IconsDir);

            foreach (var s in items)
            {
                var file = Path.Combine(IconsDir, "sale_" + SafeName(s.Id) + ".png");

                if (File.Exists(file))
                    continue;

                try
                {
                    string? url = null;

                    if (SalePages.TryGetValue(s.Id.ToLowerInvariant(), out var page))
                        url = await GetItemImageUrl(page);

                    if (url != null)
                    {
                        var bytes = await Http.GetByteArrayAsync(url);

                        using var raw = new Bitmap(new MemoryStream(bytes));
                        using var clean = CleanIcon(raw);

                        clean.Save(file, ImageFormat.Png);
                    }
                    else
                    {
                        MakePlaceholderSale(s, file);
                    }
                }
                catch { }
            }

            status("иконки продажи готовы");
        }
        catch (Exception ex)
        {
            status("иконки продажи: " + ex.Message);
        }
    }

    private static void MakePlaceholderSale(SaleItem s, string file)
    {
        using var bmp = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        int h = Math.Abs(s.Id.GetHashCode()) % 360;

        using var brush = new SolidBrush(ColorFromHsl(h, 0.55, 0.45));
        g.FillEllipse(brush, 4, 4, 56, 56);

        using var font = new Font("Segoe UI", 26, FontStyle.Bold);

        string letter = (s.Name.Length > 0 ? s.Name.Substring(0, 1) : "?").ToUpperInvariant();

        var size = g.MeasureString(letter, font);

        g.DrawString(letter, font, Brushes.White, (64 - size.Width) / 2, (64 - size.Height) / 2);

        bmp.Save(file, ImageFormat.Png);
    }

    public static string? KitIconPathFor(KitItem k)
    {
        var p = Path.Combine(IconsDir, "kit_" + SafeName(k.Id) + ".png");
        return File.Exists(p) ? p : null;
    }

    public async Task DownloadKitsAsync(List<KitItem> kits, Action<string> status)
    {
        try
        {
            Directory.CreateDirectory(IconsDir);

            foreach (var k in kits)
            {
                var file = Path.Combine(IconsDir, "kit_" + SafeName(k.Id) + ".png");

                if (File.Exists(file))
                    continue;

                try
                {
                    string? url = null;

                    if (KitPages.TryGetValue(k.Id.ToLowerInvariant(), out var page))
                        url = await GetItemImageUrl(page);

                    if (url != null)
                    {
                        var bytes = await Http.GetByteArrayAsync(url);

                        using var raw = new Bitmap(new MemoryStream(bytes));
                        using var clean = CleanIcon(raw);

                        clean.Save(file, ImageFormat.Png);
                    }
                }
                catch { }
            }

            status("иконки китов готовы");
        }
        catch (Exception ex)
        {
            status("иконки китов: " + ex.Message);
        }
    }

    public static string? IconPathFor(BuyItem b)
    {
        var p = Path.Combine(IconsDir, SafeName(b.Id) + ".png");
        return File.Exists(p) ? p : null;
    }

    public async Task DownloadAllAsync(List<BuyItem> items, Action<string> status)
    {
        try
        {
            Directory.CreateDirectory(IconsDir);

            int done = 0;

            foreach (var b in items)
            {
                var file = Path.Combine(IconsDir, SafeName(b.Id) + ".png");

                if (File.Exists(file))
                {
                    done++;
                    continue;
                }

                try
                {
                    string? url = null;

                    if (WikiPages.TryGetValue(b.Id.ToLowerInvariant(), out var page))
                        url = await GetItemImageUrl(page);

                    if (url != null)
                    {
                        var bytes = await Http.GetByteArrayAsync(url);

                        using var raw = new Bitmap(new MemoryStream(bytes));
                        using var clean = CleanIcon(raw);

                        clean.Save(file, ImageFormat.Png);
                    }
                    else
                    {
                        MakePlaceholder(b, file);
                    }

                    done++;
                    status($"иконки: {done}/{items.Count}");
                }
                catch
                {
                    try { MakePlaceholder(b, file); done++; } catch { }
                }
            }

            status($"иконки: {done}/{items.Count}");
        }
        catch (Exception ex)
        {
            status("иконки: " + ex.Message);
        }
    }

    private static async Task<string?> GetItemImageUrl(string page)
    {
        var url1 = $"https://ark.wiki.gg/api.php?action=parse&page={Uri.EscapeDataString(page)}&prop=images&format=json";

        using var doc = JsonDocument.Parse(await Http.GetStringAsync(url1));

        if (!doc.RootElement.TryGetProperty("parse", out var parse) ||
            !parse.TryGetProperty("images", out var images))
            return null;

        foreach (var im in images.EnumerateArray())
        {
            string fname = im.GetString() ?? "";

            if (!fname.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;

            var url2 = $"https://ark.wiki.gg/api.php?action=query&titles={Uri.EscapeDataString("File:" + fname)}&prop=imageinfo&iiprop=url&format=json";

            using var doc2 = JsonDocument.Parse(await Http.GetStringAsync(url2));

            if (!doc2.RootElement.TryGetProperty("query", out var q) ||
                !q.TryGetProperty("pages", out var pages))
                continue;

            foreach (var pg in pages.EnumerateObject())
            {
                if (pg.Value.TryGetProperty("imageinfo", out var ii) &&
                    ii.GetArrayLength() > 0 &&
                    ii[0].TryGetProperty("url", out var u))
                {
                    return u.GetString();
                }
            }
        }

        return null;
    }

    // уменьшает до 128px и убирает белый фон flood-fill'ом от краёв,
    // не трогая белые детали внутри иконки
    private static Bitmap CleanIcon(Bitmap src)
    {
        int w = src.Width;
        int h = src.Height;

        Bitmap work = src;

        if (w > 128 || h > 128)
        {
            double k = Math.Min(128.0 / w, 128.0 / h);
            int nw = Math.Max(16, (int)(w * k));
            int nh = Math.Max(16, (int)(h * k));

            work = new Bitmap(nw, nh);

            using (var g = Graphics.FromImage(work))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, nw, nh);
            }

            w = nw;
            h = nh;
        }

        var bmp = new Bitmap(w, h);

        using (var g = Graphics.FromImage(bmp))
        {
            g.DrawImage(work, 0, 0);
        }

        var seen = new bool[w * h];
        var stack = new Stack<int>();

        for (int x = 0; x < w; x++)
        {
            stack.Push(x);
            stack.Push((h - 1) * w + x);
        }

        for (int y = 0; y < h; y++)
        {
            stack.Push(y * w);
            stack.Push(y * w + w - 1);
        }

        while (stack.Count > 0)
        {
            int i = stack.Pop();

            if (i < 0 || i >= w * h || seen[i])
                continue;

            seen[i] = true;

            int x = i % w;
            int y = i / w;

            var c = bmp.GetPixel(x, y);

            bool isBg = c.A < 40 || (c.R > 235 && c.G > 235 && c.B > 235);

            if (!isBg)
                continue;

            bmp.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));

            if (x > 0) stack.Push(i - 1);
            if (x < w - 1) stack.Push(i + 1);
            if (y > 0) stack.Push(i - w);
            if (y < h - 1) stack.Push(i + w);
        }

        return CropToContent(bmp);
    }

    // обрезает прозрачные поля по контуру содержимого
    public static Bitmap CropToContent(Bitmap bmp)
    {
        int w = bmp.Width;
        int h = bmp.Height;

        int minX = w, minY = h, maxX = -1, maxY = -1;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (bmp.GetPixel(x, y).A > 30)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < 0)
            return bmp;

        var rect = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);

        return bmp.Clone(rect, bmp.PixelFormat);
    }

    public static Bitmap CleanAndCrop(Bitmap src)
    {
        return CleanIcon(src);
    }

    private static void MakePlaceholder(BuyItem b, string file)
    {
        using var bmp = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        int h = Math.Abs(b.Id.GetHashCode()) % 360;
        var color = ColorFromHsl(h, 0.55, 0.45);

        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 4, 4, 56, 56);

        using var font = new Font("Segoe UI", 26, FontStyle.Bold);

        string letter = (b.Name.Length > 0 ? b.Name.Substring(0, 1) : "?").ToUpperInvariant();

        var size = g.MeasureString(letter, font);

        g.DrawString(letter, font, Brushes.White, (64 - size.Width) / 2, (64 - size.Height) / 2);

        bmp.Save(file, ImageFormat.Png);
    }

    private static Color ColorFromHsl(int h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * Math.Abs(h / 60.0 % 2 - 1);
        double m = l - c / 2;

        double r, g, b;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromArgb(255,
            (int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
    }

    private static string SafeName(string s)
    {
        return string.Concat(s.Select(ch =>
            char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? char.ToLowerInvariant(ch) : '_'));
    }
}



