using AcrossReportDesigner.Models;
using Avalonia.Controls;
using System;
using System.Linq;

namespace AcrossReportDesigner.Services;

public static class ReportConverter
{
    public static ReportItem? FromBorder(Border border)
    {
        // ① Canvas / Border から「px」を取る
        double leftPx = Canvas.GetLeft(border);
        if (double.IsNaN(leftPx))
            leftPx = 0;

        double topPx = Canvas.GetTop(border);
        if (double.IsNaN(topPx))
            topPx = 0;

        double widthPx = border.Width;
        if (double.IsNaN(widthPx))
            widthPx = border.Bounds.Width;

        double heightPx = border.Height;
        if (double.IsNaN(heightPx))
            heightPx = border.Bounds.Height;

        // ★★ ここ ★★（あなたが聞いている4行）
        double leftMm = PxToMm(leftPx);
        double topMm = PxToMm(topPx);
        double widthMm = PxToMm(widthPx);
        double heightMm = PxToMm(heightPx);

        // ② Model（mm）に詰める
        Control content = border.Child is Grid g
            ? g.Children.OfType<Control>().First()
            : border.Child!;

        if (content is TextBlock tb)
            return new LabelItem
            {
                Type = "Label",
                Left = leftMm,
                Top = topMm,
                Width = widthMm,
                Height = heightMm,
                Text = tb.Text ?? ""
            };

        if (content is Avalonia.Controls.Shapes.Line line)
            return new LineItem
            {
                Type = "Line",
                Left = leftMm,
                Top = topMm,
                Width = widthMm,
                Height = heightMm,
                Thickness = PxToMm(line.StrokeThickness)
            };

        return null;
    }
    // =========================
    // px → mm 変換（Avalonia）
    // =========================
    private static double PxToMm(double px)
    {
        // Avalonia / WPF 共通：96 DPI 前提
        return px * 25.4 / 96.0;
    }
}
