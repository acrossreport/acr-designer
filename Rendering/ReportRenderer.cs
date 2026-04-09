using AcrossReportDesigner.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace AcrossReportDesigner.Rendering;

public sealed class ReportRenderer
{
    public List<RenderedPage> Render(
        ArSections sections,
        double pageWidthMm,
        double pageHeightMm,
        float dpi)
    {
        var pages = new List<RenderedPage>();

        int pageWidthPx = MmToPx(pageWidthMm, dpi);
        int pageHeightPx = MmToPx(pageHeightMm, dpi);

        var bitmap = new SKBitmap(pageWidthPx, pageHeightPx);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.White);

        double sectionTopMm = 0;

        foreach (var sec in sections.Section)
        {
            DrawSection(canvas, sec, sectionTopMm, dpi);
            sectionTopMm += sec.Height;
        }

        canvas.Flush();

        pages.Add(new RenderedPage(0, bitmap, dpi));

        return pages;
    }

    // ========================================
    // セクション描画
    // ========================================
    private void DrawSection(
        SKCanvas canvas,
        ArSection sec,
        double sectionTopMm,
        float dpi)
    {
        if (sec.Control == null)
            return;

        foreach (var c in sec.Control)
        {
            DrawControl(canvas, c, sectionTopMm, dpi);
        }
    }

    // ========================================
    // コントロール描画（重要）
    // ========================================
    private void DrawControl(
        SKCanvas canvas,
        ArControl c,
        double sectionTopMm,
        float dpi)
    {
        float x = MmToPxF(c.Left, dpi);
        float y = MmToPxF(sectionTopMm + c.Top, dpi);
        float w = MmToPxF(c.Width, dpi);
        float h = MmToPxF(c.Height, dpi);

        switch (c.Type)
        {
            case ArControlType.Text:
                DrawText(canvas, c, x, y, w, h);
                break;

            case ArControlType.Shape:
                DrawShape(canvas, c, x, y, w, h);
                break;

            case ArControlType.Image:
                DrawImage(canvas, c, x, y, w, h);
                break;
        }
    }

    // ========================================
    // Text
    // ========================================
    private void DrawText(
        SKCanvas canvas,
        ArControl c,
        float x, float y, float w, float h)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor((uint)c.ForeColor),
            TextSize = (float)c.FontSize,
            IsAntialias = true
        };

        float baseline = y + paint.TextSize;

        canvas.DrawText(
            c.Text ?? "",
            x,
            baseline,
            paint);
    }

    // ========================================
    // Shape
    // ========================================
    private void DrawShape(
        SKCanvas canvas,
        ArControl c,
        float x, float y, float w, float h)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor((uint)c.LineColor),
            IsStroke = true,
            StrokeWidth = 1,
            IsAntialias = true
        };

        canvas.DrawRect(x, y, w, h, paint);
    }

    // ========================================
    // Image（仮）
    // ========================================
    private void DrawImage(
        SKCanvas canvas,
        ArControl c,
        float x, float y, float w, float h)
    {
        using var paint = new SKPaint
        {
            Color = SKColors.Gray,
            IsStroke = true
        };

        canvas.DrawRect(x, y, w, h, paint);
    }

    // ========================================
    // mm → px
    // ========================================
    private static int MmToPx(double mm, float dpi)
        => (int)Math.Round(mm / 25.4 * dpi);

    private static float MmToPxF(double mm, float dpi)
        => (float)(mm / 25.4 * dpi);
}
