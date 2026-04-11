using AcrossReportDesigner.Models;
using AcrossReportDesigner.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;  // Rectangle
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace AcrossReportDesigner.Views;

/// <summary>
/// AR互換 Barcode コントロールのデザイナー上の描画View。
/// デザイン時はバーパターンを擬似的に描画し、種別と値を表示する。
/// </summary>
public sealed class BarcodeControlView : Border
{
    public DesignControl Model { get; }
    public double GridMm { get; set; } = 0;

    public event Action<DesignControl>? Selected;
    public event Action<DesignControl, RectMm, RectMm>? DragFinished;

    // ---- Drag / Resize ----
    private bool _dragging, _resizing;
    private Point _startPoint;
    private double _startLeft, _startTop, _startWidth, _startHeight;
    private RectMm _downRectMm;
    private const double GripSize = 10;
    private const double Dpi = 96.0;

    // ---- 内部View ----
    private readonly Canvas _barcodeCanvas;
    private readonly TextBlock _labelText;

    private readonly IBrush _normalBorder = new SolidColorBrush(Color.FromRgb(80, 80, 80));

    public BarcodeControlView(DesignControl model)
    {
        Model = model;

        BorderBrush     = _normalBorder;
        BorderThickness = new Thickness(1);
        Background      = Brushes.White;
        ClipToBounds    = true;

        // バー描画用Canvas
        _barcodeCanvas = new Canvas
        {
            IsHitTestVisible = false,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Stretch
        };

        // 種別＋値ラベル
        _labelText = new TextBlock
        {
            FontSize   = 8,
            Foreground = Brushes.Black,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Bottom,
            IsHitTestVisible    = false,
            Margin = new Thickness(0, 0, 0, 1)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Grid.SetRow(_barcodeCanvas, 0);
        Grid.SetRow(_labelText, 1);
        grid.Children.Add(_barcodeCanvas);
        grid.Children.Add(_labelText);
        Child = grid;

        PointerPressed  += OnDown;
        PointerMoved    += OnMove;
        PointerReleased += OnUp;

        ApplyPosition();
        Refresh();

        // サイズ変更時にバーを再描画
        this.SizeChanged += (_, _) => Refresh();
    }

    // -------------------------------------------------------
    // バーコード擬似描画（デザイン時ビジュアル）
    // -------------------------------------------------------
    public void Refresh()
    {
        _barcodeCanvas.Children.Clear();

        string value = string.IsNullOrEmpty(Model.BarcodeDataField)
            ? Model.BarcodeValue
            : $"[{Model.BarcodeDataField}]";

        string typeLabel = Model.BarcodeType switch
        {
            "QRCode"     => "QR",
            "JAN13"      => "JAN13",
            "EAN8"       => "EAN8",
            "NW7"        => "NW7",
            "ITF"        => "ITF",
            "PDF417"     => "PDF417",
            "DataMatrix" => "DM",
            _            => "128"
        };

        var barBrush = BrushFromOle(Model.BarColor);

        double w = Bounds.Width  > 0 ? Bounds.Width  : 80;
        double h = Bounds.Height > 0 ? Bounds.Height : 30;
        double barAreaH = Model.BarcodeShowText ? Math.Max(4, h - 14) : h;

        if (Model.BarcodeType == "QRCode")
        {
            // QR: モジュールグリッド擬似表示
            DrawQrPreview(w, barAreaH, barBrush);
        }
        else
        {
            // 線形バーコード: ランダムシードでバーパターン擬似表示
            DrawLinearBars(value, w, barAreaH, barBrush);
        }

        // 種別ラベル（左上に小さく）
        var typeTb = new TextBlock
        {
            Text       = typeLabel,
            FontSize   = 7,
            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(typeTb, 2);
        Canvas.SetTop(typeTb, 1);
        _barcodeCanvas.Children.Add(typeTb);

        // 値ラベル
        if (Model.BarcodeShowText)
        {
            _labelText.Text = value;
            _labelText.IsVisible = true;
        }
        else
        {
            _labelText.IsVisible = false;
        }
    }

    private void DrawLinearBars(string value, double w, double h, IBrush barBrush)
    {
        // 値のハッシュからバーパターンを決定論的に生成
        int seed = 0;
        foreach (char c in value) seed = seed * 31 + c;
        var rng = new Random(seed);

        double x = 4;
        double maxX = w - 4;
        while (x < maxX)
        {
            double barW = rng.NextDouble() < 0.5 ? 1.5 : 3.0;
            bool isBar  = rng.NextDouble() < 0.6;
            if (isBar)
            {
                var rect = new Rectangle
                {
                    Width  = barW,
                    Height = h,
                    Fill   = barBrush,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, 0);
                _barcodeCanvas.Children.Add(rect);
            }
            x += barW + (isBar ? 1.0 : 0.5);
        }
    }

    private void DrawQrPreview(double w, double h, IBrush barBrush)
    {
        // QR: 小さなモジュールグリッド擬似表示
        double side = Math.Min(w, h);
        double modSize = Math.Max(1, side / 21);  // QR最小21x21
        int mods = (int)(side / modSize);

        var rng = new Random(42);
        for (int row = 0; row < mods; row++)
        {
            for (int col = 0; col < mods; col++)
            {
                // 位置検出パターン（角3か所）は必ず黒
                bool isFinder = IsFinderPattern(row, col, mods);
                bool filled   = isFinder || rng.NextDouble() < 0.5;
                if (!filled) continue;

                var rect = new Rectangle
                {
                    Width  = modSize - 0.5,
                    Height = modSize - 0.5,
                    Fill   = barBrush,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(rect, col * modSize + (w - side) / 2);
                Canvas.SetTop(rect, row * modSize);
                _barcodeCanvas.Children.Add(rect);
            }
        }
    }

    private static bool IsFinderPattern(int row, int col, int mods)
    {
        // 左上・右上・左下の7x7ファインダーパターン領域
        bool inTopLeft     = row < 8 && col < 8;
        bool inTopRight    = row < 8 && col >= mods - 8;
        bool inBottomLeft  = row >= mods - 8 && col < 8;
        return inTopLeft || inTopRight || inBottomLeft;
    }

    private static IBrush BrushFromOle(int ole)
    {
        byte r = (byte)(ole & 0xFF);
        byte g = (byte)((ole >> 8) & 0xFF);
        byte b = (byte)((ole >> 16) & 0xFF);
        return new SolidColorBrush(Color.FromRgb(r, g, b));
    }

    // -------------------------------------------------------
    // 位置適用
    // -------------------------------------------------------
    public void ApplyPosition()
    {
        double pxPerMm = Dpi / 25.4;
        Canvas.SetLeft(this, Model.LeftMm * pxPerMm);
        Canvas.SetTop(this,  Model.TopMm  * pxPerMm);
        Width  = Model.WidthMm  * pxPerMm;
        Height = Model.HeightMm * pxPerMm;
    }

    public void RefreshFromModel()
    {
        ApplyPosition();
        Refresh();
    }

    // -------------------------------------------------------
    // 選択
    // -------------------------------------------------------
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            BorderBrush     = Brushes.Blue;
            BorderThickness = new Thickness(2);
        }
        else
        {
            BorderBrush     = _normalBorder;
            BorderThickness = new Thickness(1);
        }
    }

    // -------------------------------------------------------
    // Drag / Resize
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
        double leftMm   = Math.Max(0, Canvas.GetLeft(this) / pxPerMm);
        double topMm    = Math.Max(0, Canvas.GetTop(this)  / pxPerMm);
        double widthMm  = Math.Max(1, (double.IsNaN(Width)  ? Bounds.Width  : Width)  / pxPerMm);
        double heightMm = Math.Max(1, (double.IsNaN(Height) ? Bounds.Height : Height) / pxPerMm);

        if (GridMm > 0)
        {
            leftMm   = Math.Round(leftMm  / GridMm) * GridMm;
            topMm    = Math.Round(topMm   / GridMm) * GridMm;
            widthMm  = Math.Max(GridMm, Math.Round(widthMm  / GridMm) * GridMm);
            heightMm = Math.Max(GridMm, Math.Round(heightMm / GridMm) * GridMm);
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
