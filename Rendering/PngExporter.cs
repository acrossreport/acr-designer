using AcrossReportDesigner.Engines;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AcrossReportDesigner.Rendering;

public static class PngExporter  // ← これを修正
{
    private const double TwipsToPx = 96.0 / 1440.0;
    private const float BaselineRate = 0.82f;

    public static void SaveWithRenderNodes(
        string pngPath,
        List<RenderNode> nodes,
        double pageWidthTwips,
        double pageHeightTwips)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(pngPath)!);

        int widthPx = (int)(pageWidthTwips * TwipsToPx);
        int heightPx = (int)(pageHeightTwips * TwipsToPx);

        using var bitmap = new SKBitmap(widthPx, heightPx);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.White);

        foreach (var node in nodes.OrderBy(n => n.ZIndex))
        {
            DrawNode(canvas, node);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        using var fs = File.Open(pngPath, FileMode.Create, FileAccess.Write);
        data.SaveTo(fs);
    }

    private static void DrawNode(SKCanvas canvas, RenderNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Text))
            return;

        float x = (float)(node.Left * TwipsToPx);
        float y = (float)(node.Top * TwipsToPx);

        float textSizePx = (float)(System.Math.Max(6.0, node.FontSize) * (96.0 / 72.0));

        using var paint = new SKPaint
        {
            TextSize = textSizePx,
            IsAntialias = true,
            Color = ToColorRgb(node.ForeColor)
        };

        float ty = y + paint.TextSize * BaselineRate;
        canvas.DrawText(node.Text, x, ty, paint);
    }

    private static SKColor ToColorRgb(int rgb)
    {
        byte r = (byte)((rgb >> 16) & 0xFF);
        byte g = (byte)((rgb >> 8) & 0xFF);
        byte b = (byte)(rgb & 0xFF);
        return new SKColor(r, g, b);
    }
}
