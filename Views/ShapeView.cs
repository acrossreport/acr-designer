using AcrossReportDesigner.Models;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Shapes;

namespace AcrossReportDesigner.Views;

public sealed class ShapeView : Canvas
{
    public ShapeView(DesignControl model)
    {
        Width = MmToPx(model.WidthMm);
        Height = MmToPx(model.HeightMm);

        var rect = new Rectangle
        {
            Width = Width,
            Height = Height,

            // ✅ ActiveReports互換カラー（OLE_COLOR）
            Fill = BrushFromOleColor(model.BackColor),

            // ✅ 線もOLE_COLOR
            Stroke = BrushFromOleColor(model.LineColor),

            StrokeThickness = 0
        };

        Children.Add(rect);
    }

    private static double MmToPx(double mm)
    {
        return mm * 96.0 / 25.4;
    }

    // ✅✅ ActiveReports BackColor = OLE_COLOR (BGR)
    private static IBrush BrushFromOleColor(int ole)
    {
        byte r = (byte)(ole & 0xFF);
        byte g = (byte)((ole >> 8) & 0xFF);
        byte b = (byte)((ole >> 16) & 0xFF);

        return new SolidColorBrush(Color.FromArgb(255, r, g, b));
    }
}
