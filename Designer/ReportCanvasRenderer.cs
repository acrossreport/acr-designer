using AcrossReportDesigner.Models;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AcrossReportDesigner;

public sealed class ReportCanvasRenderer
{
    private const double Dpi = 96.0;

    private readonly Canvas DesignerCanvas;
    private readonly Border PageBorder;

    public ReportCanvasRenderer(Canvas canvas, Border pageBorder)
    {
        DesignerCanvas = canvas;
        PageBorder = pageBorder;
    }
        
    // =========================================================
    // ★追加：JSONが無い時でも「白紙」を必ず出す
    // =========================================================
    public void RenderEmptyPage(double paperWidthMm, double paperHeightMm)
    {
        DesignerCanvas.Children.Clear();

        PageBorder.Width = DesignerCanvas.Width = MmToPx(paperWidthMm);
        PageBorder.Height = DesignerCanvas.Height = MmToPx(paperHeightMm);

        var paper = new Rectangle
        {
            Width = DesignerCanvas.Width,
            Height = DesignerCanvas.Height,
            Fill = Brushes.White,
            Stroke = Brushes.Gray,
            StrokeThickness = 1,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(paper, 0);
        Canvas.SetTop(paper, 0);
        DesignerCanvas.Children.Add(paper);
    }

    // =========================================================
    // MainWindow.Render と同一仕様（安全移植）
    // =========================================================
    public void Render(string jsonPath)
    {
        DesignerCanvas.Children.Clear();

        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var root = doc.RootElement;

        if (!TryFind(root, "PageSettings", out var page))
            return;

        if (!TryFind(root, "Sections", out var secRoot) ||
            !TryFind(secRoot, "Section", out var secNode))
            return;

        double paperW = TwipToMm(ReadNum(page, "PaperWidth"));
        double paperH = TwipToMm(ReadNum(page, "PaperHeight"));

        if ((int)ReadNum(page, "Orientation") == 2)
            (paperW, paperH) = (paperH, paperW);

        double mL = TwipToMm(ReadNum(page, "LeftMargin"));
        double mR = TwipToMm(ReadNum(page, "RightMargin"));
        double mT = TwipToMm(ReadNum(page, "TopMargin"));
        double mB = TwipToMm(ReadNum(page, "BottomMargin"));

        PageBorder.Width = DesignerCanvas.Width = MmToPx(paperW);
        PageBorder.Height = DesignerCanvas.Height = MmToPx(paperH);

        var sections = Normalize(secNode);

        double contentH = paperH - mT - mB;
        double fixedSum = 0;
        int detailIndex = -1;

        for (int i = 0; i < sections.Count; i++)
        {
            if (ReadStr(sections[i], "Type") == "Detail")
            {
                detailIndex = i;
                continue;
            }
            fixedSum += TwipToMm(ReadNum(sections[i], "Height"));
        }

        double detailH = Math.Max(0, contentH - fixedSum);

        var topMap = new Dictionary<string, double>();
        var hMap = new Dictionary<string, double>();

        double cur = mT;
        for (int i = 0; i < sections.Count; i++)
        {
            var s = sections[i];
            string name = ReadStr(s, "Name");

            double h = TwipToMm(ReadNum(s, "Height"));
            if (i == detailIndex) h = detailH;

            topMap[name] = cur;
            hMap[name] = h;
            cur += h;
        }

        foreach (var s in sections)
        {
            DrawSectionFrame(
                topMap[ReadStr(s, "Name")],
                hMap[ReadStr(s, "Name")],
                mL,
                paperW - mL - mR
            );
        }

        foreach (var s in sections)
        {
            double secTop = topMap[ReadStr(s, "Name")];

            if (!TryGetProperty(s, "Control", out var ctrlNode))
                continue;

            foreach (var c in Enumerate(ctrlNode))
            {
                string type = ReadStr(c, "Type");

                double x = mL + TwipToMm(ReadNum(c, "Left"));
                double y = secTop + TwipToMm(ReadNum(c, "Top"));
                double w = TwipToMm(ReadNum(c, "Width"));
                double h = TwipToMm(ReadNum(c, "Height"));

                if (w <= 0 || h <= 0) continue;

                if (type == "AR.Shape" || type == "AR.Box" || type == "AR.Rectangle")
                    DrawShape(c, x, y, w, h);
                else
                    DrawText(c, x, y, w, h);
            }
        }
    }

    // ===============================
    // Shape
    // ===============================
    private void DrawShape(JsonElement c, double x, double y, double w, double h)
    {
        var r = new Rectangle
        {
            Width = MmToPx(w),
            Height = MmToPx(h),
            Stroke = ReadColor(c, "LineColor", Brushes.Black),
            StrokeThickness = 1,
            Fill = ReadBack(c),
            IsHitTestVisible = false
        };

        Canvas.SetLeft(r, MmToPx(x));
        Canvas.SetTop(r, MmToPx(y));
        DesignerCanvas.Children.Add(r);
    }

    // ===============================
    // Section frame（補助）
    // ===============================
    private void DrawSectionFrame(double y, double h, double x, double w)
    {
        if (h <= 0) return;

        var r = new Rectangle
        {
            Width = MmToPx(w),
            Height = MmToPx(h),
            Stroke = Brushes.Blue,
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double> { 1, 5 },
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(r, MmToPx(x));
        Canvas.SetTop(r, MmToPx(y));
        DesignerCanvas.Children.Add(r);
    }

    // ===============================
    // Text
    // ===============================
    private void DrawText(JsonElement c, double x, double y, double w, double h)
    {
        var box = new Rectangle
        {
            Width = MmToPx(w),
            Height = MmToPx(h),
            Stroke = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)),
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double> { 2, 2 },
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };

        var tb = new TextBlock
        {
            Text = ReadText(c),
            ClipToBounds = true,
            VerticalAlignment = VerticalAlignment.Top,
            Foreground = ReadFore(c)
        };

        ApplyStyle(tb, ReadStr(c, "Style"));

        var host = new Grid
        {
            Width = MmToPx(w),
            Height = MmToPx(h)
        };

        host.Children.Add(box);
        host.Children.Add(tb);

        Canvas.SetLeft(host, MmToPx(x));
        Canvas.SetTop(host, MmToPx(y));
        DesignerCanvas.Children.Add(host);
    }

    private static void ApplyStyle(TextBlock tb, string style)
    {
        if (string.IsNullOrWhiteSpace(style)) return;

        foreach (var part in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split(':', 2);
            if (kv.Length != 2) continue;

            string k = kv[0].Trim().ToLowerInvariant();
            string v = kv[1].Trim();

            switch (k)
            {
                case "font-family":

                    // ✅ 全角→半角補正
                    v = v.Replace("Ｍ", "M")
                         .Replace("Ｓ", "S")
                         .Replace("　", " ");

                    try
                    {
                        tb.FontFamily = new FontFamily(v);
                    }
                    catch
                    {
                        // ✅ フォントが無い場合は日本語確実フォントへ
                        tb.FontFamily = new FontFamily("Yu Gothic UI");
                    }

                    break;

                case "font-size":
                    if (v.EndsWith("pt") &&
                        double.TryParse(v[..^2], out var pt))
                        tb.FontSize = pt * Dpi / 72.0;
                    break;

                case "font-weight":
                    if (v.Contains("bold"))
                        tb.FontWeight = FontWeight.Bold;
                    break;

                case "font-style":
                    if (v.Contains("italic"))
                        tb.FontStyle = FontStyle.Italic;
                    break;

                case "text-align":
                    tb.TextAlignment = v switch
                    {
                        "center" => TextAlignment.Center,
                        "right" => TextAlignment.Right,
                        _ => TextAlignment.Left
                    };
                    break;

                case "vertical-align":
                    tb.VerticalAlignment = v switch
                    {
                        "middle" => VerticalAlignment.Center,
                        "bottom" => VerticalAlignment.Bottom,
                        _ => VerticalAlignment.Top
                    };
                    break;
            }
        }
    }

    private static IBrush ReadBack(JsonElement c)
        => ReadNum(c, "BackStyle") == 1
            ? ReadColor(c, "BackColor", Brushes.Transparent)
            : Brushes.Transparent;

    private static IBrush ReadFore(JsonElement c)
    {
        if (TryGetProperty(c, "ForeColor", out _))
            return ReadColor(c, "ForeColor", Brushes.Black);

        var styleColor = ReadStyleColor(ReadStr(c, "Style"));
        return styleColor ?? Brushes.Black;
    }

    private static IBrush? ReadStyleColor(string style)
    {
        if (string.IsNullOrWhiteSpace(style)) return null;

        foreach (var part in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split(':', 2);
            if (kv.Length == 2 &&
                kv[0].Trim().Equals("color", StringComparison.OrdinalIgnoreCase))
            {
                try { return Brush.Parse(kv[1].Trim()); }
                catch { }
            }
        }
        return null;
    }

    private static IBrush ReadColor(JsonElement c, string key, IBrush def)
    {
        if (!TryGetProperty(c, key, out var v))
            return def;

        int raw;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out raw) ||
            v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out raw))
        {
            uint argb = unchecked((uint)raw);
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            if (a == 0) a = 255;
            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        return def;
    }

    private static double TwipToMm(double twip) => twip * 25.4 / 1440.0;
    private static double MmToPx(double mm) => mm * Dpi / 25.4;

    private static bool TryGetProperty(JsonElement e, string key, out JsonElement v)
    {
        if (e.ValueKind == JsonValueKind.Object &&
            e.TryGetProperty(key, out v))
            return true;

        v = default;
        return false;
    }

    private static bool TryFind(JsonElement e, string key, out JsonElement found)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            if (e.TryGetProperty(key, out found)) return true;
            foreach (var p in e.EnumerateObject())
                if (TryFind(p.Value, key, out found)) return true;
        }
        else if (e.ValueKind == JsonValueKind.Array)
        {
            foreach (var x in e.EnumerateArray())
                if (TryFind(x, key, out found)) return true;
        }
        found = default;
        return false;
    }

    private static List<JsonElement> Normalize(JsonElement e)
    {
        var l = new List<JsonElement>();
        if (e.ValueKind == JsonValueKind.Array)
            foreach (var x in e.EnumerateArray()) l.Add(x);
        else
            l.Add(e);
        return l;
    }

    private static IEnumerable<JsonElement> Enumerate(JsonElement e)
    {
        if (e.ValueKind == JsonValueKind.Array)
            foreach (var x in e.EnumerateArray()) yield return x;
        else
            yield return e;
    }

    private static string ReadStr(JsonElement e, string key)
        => TryGetProperty(e, key, out var v) ? v.ToString() : "";

    private static double ReadNum(JsonElement e, string key)
        => TryGetProperty(e, key, out var v) &&
           double.TryParse(v.ToString(), out var d) ? d : 0;

    private static string ReadText(JsonElement c)
        => ReadStr(c, "Text") is { Length: > 0 } t ? t
         : ReadStr(c, "Text") is { Length: > 0 } cp ? cp
         : ReadStr(c, "DataField");
}
