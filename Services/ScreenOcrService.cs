using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace ArkCommander.Services;

public class ScreenOcrService
{
    private static readonly string[] RusUrls =
    {
        "https://github.com/tesseract-ocr/tessdata/raw/main/rus.traineddata",
        "https://raw.githubusercontent.com/tesseract-ocr/tessdata/main/rus.traineddata"
    };

    private static readonly HttpClient Http = new();

    public async Task<List<string>> ReadLinesAsync(
        double xPercent, double yPercent, double widthPercent, double heightPercent,
        bool lumaOnly = false)
    {
        var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;

        int x = (int)(bounds.Width * xPercent);
        int y = (int)(bounds.Height * yPercent);
        int w = (int)(bounds.Width * widthPercent);
        int h = (int)(bounds.Height * heightPercent);

        if (w <= 0 || h <= 0)
            return new List<string>();

        string debugPng = Path.Combine(AppContext.BaseDirectory, "ocr_debug.png");

        string tessdataDir = EnsureTessdata();

        using var src1 = CaptureRect(x, y, w, h);

        List<string> lines1;

        using (var prep1 = Scale(MakeGray(src1, lumaOnly ? "luma" : "green"), 3))
        {
            prep1.Save(debugPng, ImageFormat.Png);
            lines1 = OcrBitmap(prep1, tessdataDir);
        }

        if (lines1.Count == 0)
        {
            using (var prepL = Scale(MakeGray(src1, "luma"), 3))
            {
                return CleanLines(OcrBitmap(prepL, tessdataDir));
            }
        }

        await Task.Delay(300);

        using var src2 = CaptureRect(x, y, w, h);

        List<string> lines2;

        using (var prep2 = Scale(MakeGray(src2, "green"), 3))
        {
            lines2 = OcrBitmap(prep2, tessdataDir);
        }

        var stable = lines1
            .Where(l1 => lines2.Any(l2 => Norm(l2) == Norm(l1)))
            .ToList();

        return CleanLines(stable.Count > 0 ? stable : lines1);
    }

    public async Task<List<string>> ReadRedLinesAsync(
        double xPercent, double yPercent, double widthPercent, double heightPercent)
    {
        var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;

        int x = (int)(bounds.Width * xPercent);
        int y = (int)(bounds.Height * yPercent);
        int w = (int)(bounds.Width * widthPercent);
        int h = (int)(bounds.Height * heightPercent);

        if (w <= 0 || h <= 0)
            return new List<string>();

        return await Task.Run(() =>
        {
            string tessdataDir = EnsureTessdata();

            using var src = CaptureRect(x, y, w, h);

            using (var prep = Scale(MakeGray(src, "red"), 3))
            {
                prep.Save(Path.Combine(AppContext.BaseDirectory, "ocr_coords.png"), ImageFormat.Png);

                return OcrBitmap(prep, tessdataDir)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToList();
            }
        });
    }

    private string EnsureTessdata()
    {
        string tessdataDir = Path.Combine(AppContext.BaseDirectory, "tessdata");
        Directory.CreateDirectory(tessdataDir);

        string rus = Path.Combine(tessdataDir, "rus.traineddata");

        if (!File.Exists(rus))
        {
            Exception? last = null;

            foreach (var url in RusUrls)
            {
                try
                {
                    using var resp = Http.GetAsync(url).GetAwaiter().GetResult();
                    resp.EnsureSuccessStatusCode();

                    using var fs = File.Create(rus);
                    resp.Content.CopyToAsync(fs).GetAwaiter().GetResult();

                    last = null;
                    break;
                }
                catch (Exception ex)
                {
                    last = ex;
                }
            }

            if (last != null)
            {
                throw new Exception(
                    "положи rus.traineddata вручную в " + tessdataDir + " (" + last.Message + ")");
            }
        }

        return tessdataDir;
    }

    private static Bitmap CaptureRect(int x, int y, int w, int h)
    {
        var bmp = new Bitmap(w, h);

        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(x, y, 0, 0, new Size(w, h));
        }

        return bmp;
    }

    private static string Norm(string s)
    {
        var sb = new StringBuilder();

        foreach (var c in s)
        {
            if (char.IsLetter(c))
                sb.Append(char.ToLowerInvariant(c));
            else if (char.IsWhiteSpace(c))
                sb.Append(' ');
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string EdgeTrim(string s)
    {
        var t = s.Trim();

        int start = 0;
        while (start < t.Length && !char.IsLetterOrDigit(t[start]))
            start++;

        int end = t.Length;
        while (end > start && !char.IsLetterOrDigit(t[end - 1]))
            end--;

        return t.Substring(start, end - start);
    }

    private static List<string> CleanLines(List<string> lines)
    {
        var list = lines
            .Select(l => EdgeTrim(l))
            .Where(l => !string.IsNullOrEmpty(l))
            .Where(l => !IsJunk(l))
            .Distinct()
            .ToList();

        if (list.Count > 0 && list[list.Count - 1].Any(char.IsDigit))
            list.RemoveAt(list.Count - 1);

        return list;
    }

    private static bool IsJunk(string l)
    {
        if (l.IndexOf("total", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (l.IndexOf("page", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (l.IndexOf("тота", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (l.IndexOf("паге", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (Regex.IsMatch(l, @"\d\s*/"))
            return true;

        if (Regex.IsMatch(l, @"/\s*\d"))
            return true;

        if (Regex.IsMatch(l, @"^[\d\s:\-.,/|()\[\]{}]+$"))
            return true;

        if (l.Contains("[") || l.Contains("]"))
            return true;

        if (l.Any(char.IsDigit) && (l.Contains(":") || l.Contains("-")))
            return true;

        int letters = l.Count(char.IsLetter);
        int others = l.Count(c => !char.IsLetter(c) && !char.IsWhiteSpace(c));

        if (letters < 3)
            return true;

        if (others >= letters)
            return true;

        return false;
    }

    private static Bitmap MakeGray(Bitmap src, string kind)
    {
        int W = src.Width;
        int H = src.Height;

        var rect0 = new Rectangle(0, 0, W, H);
        var d0 = src.LockBits(rect0, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        int s0 = d0.Stride;
        byte[] b0 = new byte[s0 * H];
        Marshal.Copy(d0.Scan0, b0, 0, b0.Length);
        src.UnlockBits(d0);

        var bin = new Bitmap(W, H, PixelFormat.Format32bppArgb);

        var rect1 = new Rectangle(0, 0, W, H);
        var d1 = bin.LockBits(rect1, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        int s1 = d1.Stride;
        byte[] b1 = new byte[s1 * H];

        for (int y = 0; y < H; y++)
        {
            for (int xx = 0; xx < W; xx++)
            {
                int i0 = y * s0 + xx * 4;

                int r = b0[i0 + 2];
                int g = b0[i0 + 1];
                int b = b0[i0];

                int v;

                if (kind == "green")
                {
                    if (g > r + 30 && g > b + 30)
                        v = g - (r + b) / 2;
                    else
                        v = 0;
                }
                else if (kind == "red")
                {
                    if (r > g + 40 && r > b + 40)
                        v = r - (g + b) / 2;
                    else
                        v = 0;
                }
                else
                {
                    v = (r + g + b) / 3;
                }

                if (v < 0) v = 0;
                if (v > 255) v = 255;

                int i1 = y * s1 + xx * 4;

                b1[i1] = (byte)v;
                b1[i1 + 1] = (byte)v;
                b1[i1 + 2] = (byte)v;
                b1[i1 + 3] = 255;
            }
        }

        Marshal.Copy(b1, 0, d1.Scan0, b1.Length);
        bin.UnlockBits(d1);

        return bin;
    }

    private static Bitmap Scale(Bitmap src, int scale)
    {
        var big = new Bitmap(src.Width * scale, src.Height * scale);

        using (var g = Graphics.FromImage(big))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, big.Width, big.Height);
        }

        return big;
    }

    private static List<string> OcrBitmap(Bitmap bmp, string tessdataDir)
    {
        using (var engine = new TesseractEngine(tessdataDir, "rus", EngineMode.Default))
        {
            engine.SetVariable("user_defined_dpi", "300");

            using (var pix = Pix.LoadFromMemory(BitmapToBytes(bmp)))
            {
                using (var page = engine.Process(pix, PageSegMode.SingleBlock))
                {
                    return page.GetText()
                        .Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
                }
            }
        }
    }

    private static byte[] BitmapToBytes(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}

