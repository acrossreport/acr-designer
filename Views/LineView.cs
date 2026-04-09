using AcrossReportDesigner.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using System;

public sealed class LineView : Canvas
{
    private const double Dpi = 96.0;
    public DesignControl Model { get; }
    public event Action<DesignControl>? Selected;
    private readonly Line _line;
    private readonly Line _hitLine;
    private bool _dragging;
    private Point _startMouse;
    private double _startX1, _startY1, _startX2, _startY2;
    private enum DragMode { None, Move, Endpoint1, Endpoint2 }
    private DragMode _dragMode = DragMode.None;
    private const double EndpointHitPx = 10.0; // 端点の当たり判定半径

    public LineView(DesignControl model)
    {
        Model = model;

        // 実描画Line
        _line = new Line
        {
            Stroke = Brushes.Black,
            StrokeThickness = Math.Max(0.5, model.LineWidth > 0
                ? model.LineWidth * Dpi / 25.4
                : 0.5),
            IsHitTestVisible = false
        };

        // ヒットテスト用の太い透明Line（8px幅で選択しやすく）
        _hitLine = new Line
        {
            Stroke = Brushes.Transparent,
            StrokeThickness = 8,
            IsHitTestVisible = true
        };

        Children.Add(_line);
        Children.Add(_hitLine);

        _hitLine.PointerPressed  += OnPressed;
        _hitLine.PointerMoved    += OnMoved;
        _hitLine.PointerReleased += OnReleased;

        UpdateFromModel();
    }

    private static double MmToPx(double mm) => mm * Dpi / 25.4;
    private static double PxToMm(double px) => px * 25.4 / Dpi;

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        Selected?.Invoke(Model);
        SetSelected(true);

        var pos = e.GetPosition(this);  // LineView内座標
        var p1 = new Point(MmToPx(Model.X1Mm), MmToPx(Model.Y1Mm));
        var p2 = new Point(MmToPx(Model.X2Mm), MmToPx(Model.Y2Mm));

        // 端点判定
        if (Distance(pos, p1) <= EndpointHitPx)
            _dragMode = DragMode.Endpoint1;
        else if (Distance(pos, p2) <= EndpointHitPx)
            _dragMode = DragMode.Endpoint2;
        else
            _dragMode = DragMode.Move;

        _dragging = true;
        _startMouse = e.GetPosition(Parent as Visual);
        _startX1 = Model.X1Mm;
        _startY1 = Model.Y1Mm;
        _startX2 = Model.X2Mm;
        _startY2 = Model.Y2Mm;
        e.Pointer.Capture(_hitLine);
        e.Handled = true;
    }
    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        var p = e.GetPosition(Parent as Visual);
        var dx = PxToMm(p.X - _startMouse.X);
        var dy = PxToMm(p.Y - _startMouse.Y);

        switch (_dragMode)
        {
            case DragMode.Move:
                Model.X1Mm = _startX1 + dx;
                Model.Y1Mm = _startY1 + dy;
                Model.X2Mm = _startX2 + dx;
                Model.Y2Mm = _startY2 + dy;
                break;
            case DragMode.Endpoint1:
                Model.X1Mm = _startX1 + dx;
                Model.Y1Mm = _startY1 + dy;
                break;
            case DragMode.Endpoint2:
                Model.X2Mm = _startX2 + dx;
                Model.Y2Mm = _startY2 + dy;
                break;
        }

        Model.LeftMm = Math.Min(Model.X1Mm, Model.X2Mm);
        Model.TopMm = Math.Min(Model.Y1Mm, Model.Y2Mm);
        Model.WidthMm = Math.Max(1, Math.Abs(Model.X2Mm - Model.X1Mm));
        Model.HeightMm = Math.Max(1, Math.Abs(Model.Y2Mm - Model.Y1Mm));
        UpdateFromModel();
        e.Handled = true;
    }
    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        e.Pointer.Capture(null);
    }

    public void SetSelected(bool selected)
    {
        _line.Stroke = selected ? Brushes.Blue : Brushes.Black;
        _line.StrokeThickness = selected
            ? 2.0
            : Math.Max(0.5, Model.LineWidth > 0 ? Model.LineWidth * Dpi / 25.4 : 0.5);
    }
    public void UpdateFromModel()
    {
        var start = new Point(MmToPx(Model.X1Mm), MmToPx(Model.Y1Mm));
        var end   = new Point(MmToPx(Model.X2Mm), MmToPx(Model.Y2Mm));
        _line.StartPoint    = start;
        _line.EndPoint      = end;
        _hitLine.StartPoint = start;
        _hitLine.EndPoint   = end;
    }
    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
