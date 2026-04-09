using AcrossReportDesigner.Engine;
using AcrossReportDesigner.Engines;
using AcrossReportDesigner.Models;
using SkiaSharp;
using System.IO;

namespace AcrossReportDesigner.Rendering;

public sealed class PdfPngRenderer
{
    private const float Dpi = 96f;

    // ==============================
    // PDF 出力
    // ==============================
    public void ExportPdf(ReportEngine engine, string path)
    {
        using var stream = File.OpenWrite(path);
        using var document = SKDocument.CreatePdf(stream);

        float widthPx = MmToPx(engine.PrintableWidthMm);
        float heightPx = MmToPx(engine.PrintableHeightMm);

        using var canvas = document.BeginPage(widthPx, heightPx);

        canvas.Clear(SKColors.White);

        Render(engine, canvas);

        document.EndPage();
        document.Close();
    }

    // ==============================
    // PNG 出力
    // ==============================
    public void ExportPng(ReportEngine engine, string path)
    {
        int width = (int)MmToPx(engine.PrintableWidthMm);
        int height = (int)MmToPx(engine.PrintableHeightMm);

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.White);

        Render(engine, canvas);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    // ==============================
    // 共通レンダリング
    // ==============================
    private void Render(ReportEngine engine, SKCanvas canvas)
    {
        float currentY = 0f;

        foreach (var sec in engine.Sections)
        {
            RenderSection(sec, canvas, currentY);
            currentY += MmToPx(sec.HeightMm);
        }
    }

    // ==============================
    // セクション描画
    // ==============================
    private void RenderSection(
        SectionDefinition sec,
        SKCanvas canvas,
        float sectionStartY)
    {
        foreach (var ctrl in sec.Controls)
        {
            float x = MmToPx(ctrl.LeftMm);
            float y = MmToPx(ctrl.TopMm) + sectionStartY;
            float w = MmToPx(ctrl.WidthMm);
            float h = MmToPx(ctrl.HeightMm);

            // ---- 背景塗り ----
            if (ctrl.BackStyle == 1)
            {
                using var fill = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = ToColor(ctrl.BackColor)
                };
                canvas.DrawRect(x, y, w, h, fill);
            }

            // ---- 枠線 ----
            using (var stroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = ToColor(ctrl.LineColor),
                StrokeWidth = 1f,
                IsAntialias = true
            })
            {
                canvas.DrawRect(x, y, w, h, stroke);
            }

            // ---- テキスト ----
            if (!string.IsNullOrEmpty(ctrl.Text))
            {
                DrawText(canvas, ctrl, x, y, w, h);
            }

            // ---- ライン ----
            if (ctrl.Type.Contains("Line"))
            {
                using var linePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = ToColor(ctrl.LineColor),
                    StrokeWidth = 1f,
                    IsAntialias = true
                };

                canvas.DrawLine(
                    MmToPx(ctrl.X1Mm),
                    MmToPx(ctrl.Y1Mm) + sectionStartY,
                    MmToPx(ctrl.X2Mm),
                    MmToPx(ctrl.Y2Mm) + sectionStartY,
                    linePaint);
            }
        }
    }

    // ==============================
    // テキスト描画
    // ==============================
    private void DrawText(
        SKCanvas canvas,
        DesignControl ctrl,
        float x,
        float y,
        float w,
        float h)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black,
            TextSize = ctrl.FontSizePt > 0
                ? (float)(ctrl.FontSizePt * 96f / 72f)
                : 12f,
            Typeface = GetTypeface(ctrl.FontName, ctrl.Bold)
        };

        float textY = y + paint.TextSize;

        if (ctrl.TextAlign == "center")
        {
            float textWidth = paint.MeasureText(ctrl.Text);
            canvas.DrawText(ctrl.Text, x + (w - textWidth) / 2f, textY, paint);
        }
        else if (ctrl.TextAlign == "right")
        {
            float textWidth = paint.MeasureText(ctrl.Text);
            canvas.DrawText(ctrl.Text, x + w - textWidth, textY, paint);
        }
        else
        {
            canvas.DrawText(ctrl.Text, x, textY, paint);
        }
    }

    // ==============================
    // フォント
    // ==============================
    private SKTypeface GetTypeface(string? fontName, bool bold)
    {
        var weight = bold
            ? SKFontStyleWeight.Bold
            : SKFontStyleWeight.Normal;

        return SKTypeface.FromFamilyName(
            string.IsNullOrWhiteSpace(fontName)
                ? "MS Gothic"
                : fontName,
            new SKFontStyle(
                weight,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright));
    }

    // ==============================
    // ARGB変換
    // ==============================
    private SKColor ToColor(int value)
    {
        byte r = (byte)(value & 0xFF);
        byte g = (byte)((value >> 8) & 0xFF);
        byte b = (byte)((value >> 16) & 0xFF);
        return new SKColor(r, g, b);
    }

    // ==============================
    // mm → px
    // ==============================
    private float MmToPx(double mm)
    {
        return (float)(mm * Dpi / 25.4);
    }
}
