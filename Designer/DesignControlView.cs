using AcrossReportDesigner.Models;
using AcrossReportDesigner.Services;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AcrossReportDesigner.Views;

public sealed class DesignControlView : Border
{
    public DesignControl Model { get; }
    public double GridMm { get; set; } = 0;  // 0=スナップなし
    public event Action<DesignControl>? Selected;
    public event Action<DesignControl, RectMm, RectMm>? DragFinished;
    private const double Dpi = 96.0;
    private bool _dragging;
    private bool _resizing;
    private Point _startPoint;      // Parent内の開始点
    private double _startLeft;
    private double _startTop;
    private double _startWidth;
    private double _startHeight;
    private const double GripSize = 10;
    private readonly TextBlock _textBlock;
    // ✅ 通常状態を保持（選択解除で戻すため）
    private Thickness _normalBorderThickness = new Thickness(0);
    private IBrush _normalBorderBrush = Brushes.Transparent;
    private IBrush? _normalBackground;
    private readonly Rectangle _resizeGrip;
    private RectMm _downRectMm;

    public Dictionary<string, object?>? CurrentRow { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new();
    private enum GripKind
    {
        None,
        BottomRight,
        BottomLeft,
        TopRight,
        TopLeft,
        Right,   // ★追加
        Bottom,  // ★追加
        Left,    // ★追加
        Top      // ★追加
    }
    private GripKind _activeGrip = GripKind.None;
    public DesignControlView(DesignControl model)
    {
        Debug.WriteLine($"★ AddControlToCanvas {model.Type} {model.Name}");
        Model = model;

        BorderThickness = new Thickness(0);
        Background = new SolidColorBrush(Color.FromArgb(2, 0, 0, 0)); // hit test用の極薄

        var textAlignment = ResolveTextAlignment(Model);

        // ⭐ここ修正（初期Textは入れない）
        _textBlock = new TextBlock
        {
            Text = "",   // ←ここが最重要修正
            Foreground = Brushes.Black,
            FontFamily = new FontFamily(
                string.IsNullOrWhiteSpace(Model.FontName)
                    ? "MS Gothic"
                    : Model.FontName),
            FontSize = Model.FontSizePt > 0
                ? PtToPx(Model.FontSizePt)
                : 12,
            FontWeight = Model.Bold ? FontWeight.Bold : FontWeight.Normal,
            TextAlignment = textAlignment,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.None,
            IsHitTestVisible = false
        };

        // Child設定
        var grid = new Grid();
        grid.Children.Add(_textBlock);
        Child = grid;

        // ========= 通常枠 =========
        if (IsLabel(Model))
        {
            _normalBorderBrush = new SolidColorBrush(Color.FromRgb(190, 190, 190));
            _normalBorderThickness = new Thickness(1);
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // ★Transparentをやめる
        }
        else if (IsTextBoxLike(Model))
        {
            _normalBorderBrush = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            _normalBorderThickness = new Thickness(1);
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // ★Transparentをやめる
        }
        else
        {
            _normalBorderBrush = Brushes.Transparent;
            _normalBorderThickness = new Thickness(0);
        }

        BorderBrush = _normalBorderBrush;
        BorderThickness = _normalBorderThickness;

        Padding = new Thickness(0);

        // ========= Shape =========
        if (Model.Type.Contains("Shape"))
        {
            if (Model.BackStyle == 1)
                Background = BrushFromOleColor(Model.BackColor);

            BorderBrush = BrushFromOleColor(Model.LineColor);
            BorderThickness = new Thickness(1);
        }

        _normalBackground = Background;

        PointerPressed += OnDown;
        Debug.WriteLine($"★ PointerPressed登録完了: {Model.Name}");
        PointerMoved += OnMove;
        PointerReleased += OnUp;
        ApplyPosition();
        // ⭐ここ追加（最重要）
        UpdateText();

        // ★テスト：Attachedイベントで確認
        this.AttachedToVisualTree += (s, e) =>
        {
            Debug.WriteLine($"★ AttachedToVisualTree: {Model.Name} IsHitTestVisible={IsHitTestVisible}");
        };
    }
    // =====================================================
    // ✅ TextAlignment 解決（TextAlign優先→Style→Left）
    // =====================================================
    private static TextAlignment ResolveTextAlignment(DesignControl model)
    {
        string? a = NormalizeAlignToken(model.TextAlign);
        if (a == null)
        {
            string? styleAlign = GetStyleValue(model.Style, "text-align");
            a = NormalizeAlignToken(styleAlign);
        }
        return a switch
        {
            "center" => TextAlignment.Center,
            "right" => TextAlignment.Right,
            _ => TextAlignment.Left
        };
    }
    private static string? NormalizeAlignToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        string s = raw.Trim().ToLowerInvariant();
        if (s == "left" || s == "center" || s == "right") return s;
        if (s == "1") return "left";
        if (s == "2") return "center";
        if (s == "3") return "right";
        return null;
    }
    private static string? GetStyleValue(string? style, string key)
    {
        if (string.IsNullOrWhiteSpace(style))
            return null;
        string targetKey = key.Trim().ToLowerInvariant();
        foreach (var raw in style.Split(';'))
        {
            var s = raw.Trim();
            if (s.Length == 0)
                continue;
            int idx = s.IndexOf(':');
            if (idx < 0)
                continue;
            string k = s.Substring(0, idx).Trim().ToLowerInvariant();
            if (k != targetKey)
                continue;
            return s.Substring(idx + 1).Trim();
        }
        return null;
    }
    // =========================
    // Layout
    // =========================
    public void ApplyPosition()
    {
        double pxPerMm = 96.0 / 25.4;
        Canvas.SetLeft(this, Model.LeftMm * pxPerMm);
        Canvas.SetTop(this, Model.TopMm * pxPerMm);
        Width = Model.WidthMm * pxPerMm;
        Height = Model.HeightMm * pxPerMm;
    }
    // =========================
    // Drag / Resize
    // =========================
    private void OnDown(object? sender, PointerPressedEventArgs e)
    {
        Debug.WriteLine("★★★ OnDown 呼ばれた ★★★"); // ★追加
        Debug.WriteLine("DesignControlView CLICK");
        _activeGrip = GetGrip(e.GetPosition(this));
        // ★デバッグ追加
        Debug.WriteLine($"OnDown Bounds=({Bounds.Width:F1},{Bounds.Height:F1}) Width={Width:F1} Height={Height:F1}");
        Debug.WriteLine($"OnDown pos={e.GetPosition(this)} activeGrip={_activeGrip}");
        Focus();
        Selected?.Invoke(Model);
        var parent = this.Parent as Control;
        if (parent == null) return;
        _activeGrip = GetGrip(e.GetPosition(this));
        _startPoint = e.GetPosition(parent);
        var left = Canvas.GetLeft(this);
        var top = Canvas.GetTop(this);
        _startLeft = double.IsNaN(left) ? 0 : left;
        _startTop = double.IsNaN(top) ? 0 : top;
        _startWidth = Bounds.Width;
        _startHeight = Bounds.Height;
        _resizing = _activeGrip != GripKind.None;
        _dragging = !_resizing;
        // ★ Undo用に保存
        _downRectMm = new RectMm(
            Model.LeftMm,
            Model.TopMm,
            Model.WidthMm,
            Model.HeightMm);
        e.Pointer.Capture(this);
        e.Handled = true;
    }
    private void OnMove(object? sender, PointerEventArgs e)
    {
        var parent = this.Parent as Control;
        if (parent == null) return;
        var pParent = e.GetPosition(parent);
        var pSelf = e.GetPosition(this);
        // カーソル変更のみ（非ドラッグ時）
        if (!_dragging && !_resizing)
        {
            switch (GetGrip(pSelf))
            {
                case GripKind.BottomRight:
                case GripKind.TopLeft:
                    Cursor = new Cursor(StandardCursorType.TopLeftCorner);
                    break;
                case GripKind.BottomLeft:
                case GripKind.TopRight:
                    Cursor = new Cursor(StandardCursorType.TopRightCorner);
                    break;
                case GripKind.Right:
                case GripKind.Left:
                    Cursor = new Cursor(StandardCursorType.SizeWestEast);
                    break;
                case GripKind.Bottom:
                case GripKind.Top:
                    Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                    break;
                default:
                    Cursor = new Cursor(StandardCursorType.Arrow);
                    break;
            }
            return;
        }

        double dx = pParent.X - _startPoint.X;
        double dy = pParent.Y - _startPoint.Y;

        // 移動
        if (_dragging)
        {
            Canvas.SetLeft(this, _startLeft + dx);
            Canvas.SetTop(this, _startTop + dy);
            e.Handled = true;
            return;
        }

        if (_resizing)
        {
            double newLeft = _startLeft;
            double newTop = _startTop;
            double newW = _startWidth;
            double newH = _startHeight;
            const double minSize = 8;

            switch (_activeGrip)
            {
                case GripKind.BottomRight:
                    newW = Math.Max(minSize, _startWidth + dx);
                    newH = Math.Max(minSize, _startHeight + dy);
                    break;
                case GripKind.BottomLeft:
                    newW = Math.Max(minSize, _startWidth - dx);
                    newH = Math.Max(minSize, _startHeight + dy);
                    newLeft = Math.Max(0, _startLeft + dx);  // ★
                    break;
                case GripKind.TopRight:
                    newW = Math.Max(minSize, _startWidth + dx);
                    newH = Math.Max(minSize, _startHeight - dy);
                    newTop = Math.Max(0, _startTop + dy);    // ★
                    break;
                case GripKind.TopLeft:
                    newW = Math.Max(minSize, _startWidth - dx);
                    newH = Math.Max(minSize, _startHeight - dy);
                    newLeft = Math.Max(0, _startLeft + dx);  // ★
                    newTop = Math.Max(0, _startTop + dy);   // ★
                    break;
                case GripKind.Right:
                    newW = Math.Max(minSize, _startWidth + dx);
                    break;
                case GripKind.Left:
                    newW = Math.Max(minSize, _startWidth - dx);
                    newLeft = Math.Max(0, _startLeft + dx);  // ★
                    break;
                case GripKind.Bottom:
                    newH = Math.Max(minSize, _startHeight + dy);
                    break;
                case GripKind.Top:
                    newH = Math.Max(minSize, _startHeight - dy);
                    newTop = Math.Max(0, _startTop + dy);    // ★
                    break;
            }

            Width = newW;
            Height = newH;
            Canvas.SetLeft(this, newLeft);
            Canvas.SetTop(this, newTop);
            InvalidateMeasure();
            InvalidateArrange();
            e.Handled = true;
        }
    }
    private void OnUp(object? sender, PointerReleasedEventArgs e)
    {
        bool wasResizing = _resizing;  // ★保存
        _dragging = false;
        _resizing = false;
        _activeGrip = GripKind.None;
        e.Pointer.Capture(null);

        double pxPerMm = 96.0 / 25.4;
        double leftPx = Canvas.GetLeft(this);
        double topPx = Canvas.GetTop(this);
        double widthPx = double.IsNaN(Width) ? Bounds.Width : Width;
        double heightPx = double.IsNaN(Height) ? Bounds.Height : Height;

        double leftMm = Math.Max(0, double.IsNaN(leftPx) ? 0 : leftPx / pxPerMm);
        double topMm = Math.Max(0, double.IsNaN(topPx) ? 0 : topPx / pxPerMm);
        double widthMm = Math.Max(1.0, widthPx / pxPerMm);
        double heightMm = Math.Max(1.0, heightPx / pxPerMm);

        // ★ グリッドスナップ適用
        if (GridMm > 0)
        {
            leftMm = Math.Round(leftMm / GridMm) * GridMm;
            topMm = Math.Round(topMm / GridMm) * GridMm;
            widthMm = Math.Max(GridMm, Math.Round(widthMm / GridMm) * GridMm);
            heightMm = Math.Max(GridMm, Math.Round(heightMm / GridMm) * GridMm);
        }
        leftMm = Math.Max(0, leftMm);
        topMm = Math.Max(0, topMm);

        var newRect = new RectMm(leftMm, topMm, widthMm, heightMm);

        if (!_downRectMm.EqualsApprox(newRect))
        {
            Model.LeftMm = newRect.Left;
            Model.TopMm = newRect.Top;
            Model.WidthMm = newRect.Width;
            Model.HeightMm = newRect.Height;
            DragFinished?.Invoke(Model, _downRectMm, newRect);
        }
    }
    // =========================
    // Selection（色がおかしい原因をここで止める）
    // =========================
    public void SetSelected(bool selected)
    {
        Debug.WriteLine($"SetSelected {Model.Name} = {selected}");
        if (selected)
        {
            BorderBrush = Brushes.Blue;
            BorderThickness = new Thickness(2);
            // ✅ Shapeの塗りを壊さない（ここが重要）
            if (!Model.Type.Contains("Shape"))
            {
                Background = new SolidColorBrush(Color.FromArgb(25, 0, 120, 255));
            }
        }
        else
        {
            BorderBrush = _normalBorderBrush;
            BorderThickness = _normalBorderThickness;
            // ✅ 透明固定に戻さず、通常背景へ復帰
            Background = _normalBackground ?? Brushes.Transparent;
        }
    }
    // =========================
    // AR互換 表示文字
    // =========================
    private string ResolveDisplayText(
        DesignControl model,
        Dictionary<string, object?>? row,
        Dictionary<string, object?> parameters)
    {
        if (!string.IsNullOrWhiteSpace(model.DataField))
        {
            if (row != null &&
                row.TryGetValue(model.DataField, out var v))
                return v?.ToString() ?? "";
            if (parameters.TryGetValue(model.DataField, out var p))
                return p?.ToString() ?? "";
        }
        if (!string.IsNullOrWhiteSpace(model.Text))
            return model.Text;
        // ② TextBox も名前を表示（Label と同様）
        if (IsTextBoxLike(model))
            return $"[{model.Name}]";
        return "";
    }
    public void RefreshText()
    {
        UpdateText();
    }
    // =========================
    // Helpers
    // =========================
    private static bool IsLabel(DesignControl model)
        => model.Type.Contains("Label");
    private static bool IsTextBoxLike(DesignControl model)
        => model.Type.Contains("TextBox") || model.Type.Contains("Field");
    private static double PtToPx(double pt)
    {
        return pt * 96.0 / 72.0;
    }
    private static IBrush BrushFromOleColor(int ole)
    {
        byte r = (byte)(ole & 0xFF);
        byte g = (byte)((ole >> 8) & 0xFF);
        byte b = (byte)((ole >> 16) & 0xFF);
        return new SolidColorBrush(Color.FromArgb(255, r, g, b));
    }
    private GripKind GetGrip(Point p)
    {
        double w = Width;   // ★ Bounds.Width → Width
        double h = Height;  // ★ Bounds.Height → Height

        bool left = p.X <= GripSize;
        bool right = p.X >= w - GripSize;
        bool top = p.Y <= GripSize;
        bool bottom = p.Y >= h - GripSize;

        // 四隅（優先）
        if (right && bottom) return GripKind.BottomRight;
        if (left && bottom) return GripKind.BottomLeft;
        if (right && top) return GripKind.TopRight;
        if (left && top) return GripKind.TopLeft;

        // 辺（四隅の次に判定）
        if (right) return GripKind.Right;
        if (left) return GripKind.Left;
        if (bottom) return GripKind.Bottom;
        if (top) return GripKind.Top;

        return GripKind.None;
    }
    public void UpdateText()
    {
        if (Model == null) return;
        string text = ResolveDisplayText(Model, CurrentRow, Parameters);
        _textBlock.Text = text;
    }
    public void RefreshFromModel()
    {
        Canvas.SetLeft(this, UnitConverter.MmToPx(Model.LeftMm));
        Canvas.SetTop(this, UnitConverter.MmToPx(Model.TopMm));
        Width = UnitConverter.MmToPx(Model.WidthMm);
        Height = UnitConverter.MmToPx(Model.HeightMm);
        UpdateText();
        _textBlock.FontFamily = new FontFamily(
            string.IsNullOrWhiteSpace(Model.FontName) ? "MS Gothic" : Model.FontName);
        _textBlock.FontSize = Model.FontSizePt > 0 ? PtToPx(Model.FontSizePt) : 12;
        _textBlock.FontWeight = Model.Bold ? FontWeight.Bold : FontWeight.Normal;

        // ★罫線反映
        if (Model.LineWidth > 0)
        {
            BorderThickness = new Thickness(Model.LineWidth);
            BorderBrush = BrushFromOleColor(Model.LineColor);
        }
        else
        {
            BorderThickness = _normalBorderThickness;
            BorderBrush = _normalBorderBrush;
        }
    }
    private RectMm GetRectMmFromCurrent()
    {
        double pxPerMm = 96.0 / 25.4;
        double leftMm = Canvas.GetLeft(this) / pxPerMm;
        double topMm = Canvas.GetTop(this) / pxPerMm;
        double wMm = Bounds.Width / pxPerMm;
        double hMm = Bounds.Height / pxPerMm;
        return new RectMm(leftMm, topMm, wMm, hMm);
    }
}
