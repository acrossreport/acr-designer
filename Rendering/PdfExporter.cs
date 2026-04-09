using AcrossReportDesigner.Engines;
using AcrossReportDesigner.Models;
using AcrossReportDesigner.Services;  // ← これを追加
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;  // ← これを追加

namespace AcrossReportDesigner.Rendering
{
    public static class PdfExporter
    {
        // 単位変換
        private const double TwipsToPx = 96.0 / 1440.0;
        private const float BaselineRate = 0.82f;

        // フォント組み込み PDF 出力
        public static void SaveWithEmbeddedFont(
            string pdfPath,
            List<RenderNode> nodes,
            double pageWidthTwips,
            double pageHeightTwips,
            FontMode fallbackFontMode)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);

            using var stream = File.Open(pdfPath, FileMode.Create);
            using var doc = SKDocument.CreatePdf(stream);

            int pageWidthPx = (int)(pageWidthTwips * TwipsToPx);
            int pageHeightPx = (int)(pageHeightTwips * TwipsToPx);

            var groupedPages = nodes
                .GroupBy(n => n.PageIndex)
                .OrderBy(g => g.Key);  // LINQで並べ替え

            var typefaceCache = new Dictionary<FontMode, SKTypeface>();

            foreach (var page in groupedPages)
            {
                using var canvas = doc.BeginPage(pageWidthPx, pageHeightPx);

                foreach (var node in page.OrderBy(n => n.ZIndex))  // LINQでZIndex順に並べ替え
                {
                    DrawNode(canvas, node, fallbackFontMode, typefaceCache);
                }

                doc.EndPage();
            }

            doc.Close();
        }

        private static void DrawNode(
            SKCanvas canvas,
            RenderNode node,
            FontMode fallbackMode,
            Dictionary<FontMode, SKTypeface> typefaceCache)
        {
            if (string.IsNullOrWhiteSpace(node.Text))
                return;

            FontMode mode = ResolveFontModeFromFontName(node.FontName, fallbackMode);

            if (!typefaceCache.TryGetValue(mode, out var typeface))
            {
                typeface = FontManager.GetTypeface(mode);  // FontManagerを使っている
                typefaceCache[mode] = typeface;
            }

            float x = (float)(node.Left * TwipsToPx);
            float y = (float)(node.Top * TwipsToPx);

            float textSizePx = (float)(Math.Max(6.0, node.FontSize) * (96.0 / 72.0));

            using var paint = new SKPaint
            {
                Typeface = typeface,
                TextSize = textSizePx,
                IsAntialias = true,
                SubpixelText = true,
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
        private static FontMode ResolveFontModeFromFontName(
    string fontName,
    FontMode fallbackMode)
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return fallbackMode;

            string f = fontName.Trim();

            bool isBold =
                f.Contains("Bold", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("太字", StringComparison.OrdinalIgnoreCase);

            bool isSerif =
                f.Contains("Serif", StringComparison.OrdinalIgnoreCase);

            bool isSans =
                f.Contains("Sans", StringComparison.OrdinalIgnoreCase);

            if (isSerif)
                return isBold ? FontMode.NotoSerifBold : FontMode.NotoSerif;

            if (isSans)
                return isBold ? FontMode.NotoSansBold : FontMode.NotoSans;

            return fallbackMode;
        }

    }
}
