using AcrossReportDesigner.Models;
using AcrossReportDesigner.Services;
using AcrossReportDesigner.Views;
using AcrossReportDesigner.Engine;
using Avalonia.Controls;
using System.Collections.Generic;

namespace AcrossReportDesigner.Engines;

public sealed class DesignerRenderer
{
    private readonly Canvas _canvas;

    public DesignerRenderer(Canvas canvas)
    {
        _canvas = canvas;
    }

    public void Render(ReportEngine engine)
    {
        _canvas.Children.Clear();

        double printableWidthPx =
            UnitConverter.MmToPx(engine.PrintableWidthMm);

        foreach (var sec in engine.Sections)
        {
            RenderSection(sec, printableWidthPx);
        }
    }

    private void RenderSection(
        SectionDefinition sec,
        double printableWidthPx)
    {
        var canvas = new Canvas
        {
            Width = printableWidthPx,
            Height = UnitConverter.MmToPx(sec.HeightMm)
        };

        foreach (var ctrl in sec.Controls)
        {
            var view = new DesignControlView(ctrl);

            Canvas.SetLeft(view,
                UnitConverter.MmToPx(ctrl.LeftMm));

            Canvas.SetTop(view,
                UnitConverter.MmToPx(ctrl.TopMm));

            view.Width =
                UnitConverter.MmToPx(ctrl.WidthMm);

            view.Height =
                UnitConverter.MmToPx(ctrl.HeightMm);

            canvas.Children.Add(view);
        }

        _canvas.Children.Add(canvas);
    }
}
