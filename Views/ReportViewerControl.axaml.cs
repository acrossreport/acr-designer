using AcrossReportDesigner.Viewer;
using Avalonia.Controls;

namespace AcrossReportDesigner.Views;

public partial class ReportViewerControl : UserControl
{
    private readonly ViewerEngine _engine = new();
    
  

    public ReportViewerControl()
    {
        InitializeComponent();

        _engine.StateChanged += Refresh;

        PrevButton.Click += (_, _) => _engine.Prev();
        NextButton.Click += (_, _) => _engine.Next();
        ZoomInButton.Click += (_, _) => _engine.SetZoom(_engine.Zoom * 1.25);
        ZoomOutButton.Click += (_, _) => _engine.SetZoom(_engine.Zoom / 1.25);

        Refresh();
    }

    public ViewerEngine Engine => _engine;

    private void Refresh()
    {
        PrevButton.IsEnabled = _engine.CanPrev;
        NextButton.IsEnabled = _engine.CanNext;

        PageInfo.Text = _engine.PageCount == 0
            ? "0 / 0"
            : $"{_engine.PageIndex + 1} / {_engine.PageCount}";

        ZoomInfo.Text = $"{_engine.Zoom:0.##}x";
        PageImage.Source = _engine.GetAvaloniaBitmapCurrent();
    }
}
