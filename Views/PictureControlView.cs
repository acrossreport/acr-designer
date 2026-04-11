using AcrossReportDesigner.Models;
using AcrossReportDesigner.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Diagnostics;

namespace AcrossReportDesigner.Views;

/// <summary>
/// AR互換 PictureBox のデザイナー上の描画View。
/// DesignControlView と同じ Drag/Resize/Select 機構を持つ。
/// </summary>
public sealed class PictureControlView : Border
{
    public DesignControl Model { get; }
    public double GridMm { get; set; } = 0;

    public event Action<DesignControl>? Selected;
    public event Action<DesignControl, RectMm, RectMm>? DragFinished;

    // ---- Drag / Resize ----
    private bool _dragging;
    private bool _resizing;
    private Point _startPoint;
    private double _startLeft, _startTop, _startWidth, _startHeight;
    private RectMm _downRectMm;
    private const double GripSize = 10;
    private const double Dpi = 96.0;

    // ---- 内部View ----
    private readonly Image _image;
    private readonly TextBlock _placeholder;

    // ---- 選択状態保存 ----
    private readonly IBrush _normalBorder = new SolidColorBrush(Color.FromRgb(100, 160, 220));
    private const double NormalThickness = 1.2;

    public PictureControlView(DesignControl model)
    {
        Model = model;

        // 枠線（AR: 水色系でPictureBoxらしく）
        BorderBrush = _normalBorder;
        BorderThickness = new Thickness(NormalThickness);
        Background = new SolidColorBrush(Color.FromArgb(15, 100, 160, 220));
        ClipToBounds = true;

        // 画像表示
        _image = new Image
        {
            IsHitTestVisible = false,
            Stretch = ResolveStretch(model.SizeMode)
        };

        // プレースホルダー（画像未設定時）
        _placeholder = new TextBlock
        {
            Text = string.IsNullOrEmpty(model.ImagePath)
                ? $"🖼 {model.Name}"
                : System.IO.Path.GetFileName(model.ImagePath),
            Foreground = new SolidColorBrush(Color.FromRgb(80, 120, 180)),
            FontSize = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            IsHitTestVisible = false
        };

        var grid = new Grid();
        grid.Children.Add(_image);
        grid.Children.Add(_placeholder);
        Child = grid;

        // 画像ロード試行
        LoadImage(model.ImagePath);

        PointerPressed  += OnDown;
        PointerMoved    += OnMove;
        PointerReleased += OnUp;
        ApplyPosition();
    }

    // -------------------------------------------------------
    // 画像ロード
    // -------------------------------------------------------
    public void LoadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            _image.Source = null;
            _placeholder.IsVisible = true;
            return;
        }
        try
        {
            _image.Source = new Bitmap(path);
            _image.Stretch = ResolveStretch(Model.SizeMode);
            _placeholder.IsVisible = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PictureControlView LoadImage ERROR: {ex.Message}");
            _image.Source = null;
            _placeholder.IsVisible = true;
        }
    }

    private static Stretch ResolveStretch(int sizeMode) => sizeMode switch
    {
        0 => Stretch.None,          // Clip
        1 => Stretch.Fill,          // Stretch
        2 => Stretch.Uniform,       // Zoom（AR既定）
        3 => Stretch.None,          // Center（Avalonia側で中央寄せ）
        _ => Stretch.Uniform
    };

    // -------------------------------------------------------
    // 位置適用
    // -------------------------------------------------------
    public void ApplyPosition()
    {
        double pxPerMm = Dpi / 25.4;
        Canvas.SetLeft(this, Model.LeftMm * pxPerMm);
        Canvas.SetTop(this, Model.TopMm * pxPerMm);
        Width  = Model.WidthMm  * pxPerMm;
        Height = Model.HeightMm * pxPerMm;
    }

    public void RefreshFromModel()
    {
        ApplyPosition();
        _image.Stretch = ResolveStretch(Model.SizeMode);
        LoadImage(Model.ImagePath);
        _placeholder.Text = string.IsNullOrEmpty(Model.ImagePath)
            ? $"🖼 {Model.Name}"
            : System.IO.Path.GetFileName(Model.ImagePath);
    }

    // -------------------------------------------------------
    // 選択
    // -------------------------------------------------------
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            BorderBrush = Brushes.Blue;
            BorderThickness = new Thickness(2);
            Background = new SolidColorBrush(Color.FromArgb(25, 0, 120, 255));
        }
        else
        {
            BorderBrush = _normalBorder;
            BorderThickness = new Thickness(NormalThickness);
            Background = new SolidColorBrush(Color.FromArgb(15, 100, 160, 220));
        }
    }

    // -------------------------------------------------------
    // Drag / Resize（DesignControlView と同一ロジック）
    // -------------------------------------------------------
    private void OnDown(object? sender, PointerPressedEventArgs e)
    {
        Selected?.Invoke(Model);
        var parent = this.Parent as Control;
        if (parent == null) return;

        _startPoint = e.GetPosition(parent);
        var left = Canvas.GetLeft(this);
        var top  = Canvas.GetTop(this);
        _startLeft   = double.IsNaN(left) ? 0 : left;
        _startTop    = double.IsNaN(top)  ? 0 : top;
        _startWidth  = Bounds.Width;
        _startHeight = Bounds.Height;

        bool onGrip = IsOnResizeGrip(e.GetPosition(this));
        _resizing = onGrip;
        _dragging = !onGrip;

        _downRectMm = new RectMm(Model.LeftMm, Model.TopMm, Model.WidthMm, Model.HeightMm);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnMove(object? sender, PointerEventArgs e)
    {
        var parent = this.Parent as Control;
        if (parent == null) return;
        var pParent = e.GetPosition(parent);

        double dx = pParent.X - _startPoint.X;
        double dy = pParent.Y - _startPoint.Y;

        if (_dragging)
        {
            Canvas.SetLeft(this, _startLeft + dx);
            Canvas.SetTop(this,  _startTop  + dy);
            e.Handled = true;
        }
        else if (_resizing)
        {
            Width  = Math.Max(8, _startWidth  + dx);
            Height = Math.Max(8, _startHeight + dy);
            e.Handled = true;
        }
        else
        {
            Cursor = IsOnResizeGrip(e.GetPosition(this))
                ? new Cursor(StandardCursorType.BottomRightCorner)
                : new Cursor(StandardCursorType.Arrow);
        }
    }

    private void OnUp(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = _resizing = false;
        e.Pointer.Capture(null);

        double pxPerMm = Dpi / 25.4;
        double leftMm  = Math.Max(0, Canvas.GetLeft(this) / pxPerMm);
        double topMm   = Math.Max(0, Canvas.GetTop(this)  / pxPerMm);
        double widthMm = Math.Max(1, (double.IsNaN(Width)  ? Bounds.Width  : Width)  / pxPerMm);
        double heightMm= Math.Max(1, (double.IsNaN(Height) ? Bounds.Height : Height) / pxPerMm);

        if (GridMm > 0)
        {
            leftMm  = Math.Round(leftMm  / GridMm) * GridMm;
            topMm   = Math.Round(topMm   / GridMm) * GridMm;
            widthMm = Math.Max(GridMm, Math.Round(widthMm  / GridMm) * GridMm);
            heightMm= Math.Max(GridMm, Math.Round(heightMm / GridMm) * GridMm);
        }

        var newRect = new RectMm(leftMm, topMm, widthMm, heightMm);
        if (!_downRectMm.EqualsApprox(newRect))
        {
            Model.LeftMm   = newRect.Left;
            Model.TopMm    = newRect.Top;
            Model.WidthMm  = newRect.Width;
            Model.HeightMm = newRect.Height;
            DragFinished?.Invoke(Model, _downRectMm, newRect);
        }
    }

    private bool IsOnResizeGrip(Point p)
        => p.X >= Width - GripSize && p.Y >= Height - GripSize;
}
