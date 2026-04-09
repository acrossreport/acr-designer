using AcrossReportDesigner.Models;
using SkiaSharp;
using System.Collections.Generic;
using System.Diagnostics;

namespace AcrossReportDesigner.Services
{
    public static class SkiaRenderer
    {
        public static void RenderPages(
            List<List<SectionNode>> pages,
            double pageWidthTwips,
            double pageHeightTwips,
            double topMarginTwips,
            double leftMarginTwips,
            double scale)
        {
            Debug.WriteLine("=== PAGE SIZE ===");
            Debug.WriteLine($"PageWidthTwips = {pageWidthTwips}");
            Debug.WriteLine($"PageHeightTwips = {pageHeightTwips}");

            foreach (var page in pages)
            {
                // ページの描画
                int pageWidthPx = (int)(pageWidthTwips * 96.0 / 1440.0); // ページ幅
                int pageHeightPx = (int)(pageHeightTwips * 96.0 / 1440.0); // ページ高さ

                var bitmap = new SKBitmap(pageWidthPx, pageHeightPx);
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.White); // 背景を白で塗りつぶし

                    double yOffset = topMarginTwips;

                    foreach (var section in page)
                    {
                        // セクションごとの描画処理
                        if (yOffset + section.Height > pageHeightTwips - topMarginTwips)
                            break; // ページに収まらない場合は次のページに進む

                        DrawSection(canvas, section, yOffset, leftMarginTwips, scale);
                        yOffset += section.Height;
                    }
                }
            }
        }

        private static void DrawSection(SKCanvas canvas, SectionNode section, double yOffset, double leftMarginTwips, double scale)
        {
            // セクションを描画する処理
            foreach (var control in section.Controls)
            {
                // Control を描画
                float x = (float)(leftMarginTwips + control.Left * scale);
                float y = (float)(yOffset + control.Top * scale);
                float width = (float)(control.Width * scale);
                float height = (float)(control.Height * scale);

                var paint = new SKPaint
                {
                    Color = ToSKColor(control.BackColor),
                    Style = SKPaintStyle.Fill
                };

                // 背景の描画
                canvas.DrawRect(x, y, width, height, paint);

                // テキスト描画
                if (!string.IsNullOrEmpty(control.Text))
                {
                    paint.Style = SKPaintStyle.Stroke;
                    paint.TextSize = (float)(control.FontSize * scale);
                    paint.Color = ToSKColor(control.ForeColor);
                    canvas.DrawText(control.Text, x + 2, y + height / 2, paint);
                }
            }
        }

        private static SKColor ToSKColor(int rgb)
        {
            byte r = (byte)((rgb >> 16) & 0xFF);
            byte g = (byte)((rgb >> 8) & 0xFF);
            byte b = (byte)(rgb & 0xFF);
            return new SKColor(r, g, b);
        }
    }
}
