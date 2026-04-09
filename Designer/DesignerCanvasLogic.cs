using AcrossReportDesigner.Models;
using AcrossReportDesigner.Services;
using AcrossReportDesigner.UndoRedo;
using AcrossReportDesigner.Views;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.ObjectModel;

namespace AcrossReportDesigner.Designer;

public sealed class DesignerCanvasLogic
{
    public Action<string, bool>? StatusHandler;
    private const double Dpi = 96.0;
    private DesignControl? _currentSelected;
    //private bool _outlineDirty = true;
    private double _unitsPerInch = 1440.0; // fallback: twips
    private double UnitsPerMm => _unitsPerInch / 25.4;
    public List<OutlineNode> OutlineRoots { get; private set; } = new();
    private readonly Dictionary<DesignControl, DesignControlView> _controlViewMap = new();
    private readonly ObservableCollection<OutlineNode> _outlineCollection = new();
    public ObservableCollection<OutlineNode> OutlineCollection => _outlineCollection;
    // =====================================================
    // ✅ ActiveReportsルート保持（キー揺れ対応）
    // =====================================================
    private string _rootKeyName = "AcReport";
    private JsonNode? _rootJson;
    private JsonNode? _sourceJsonNode;
    // ✅ ページ内部に描画するCanvas
    private readonly Canvas _pageCanvas;
    // ✅ ページ枠
    private readonly Border _pageBorder;
    public event Action<DesignControl>? ControlSelected;
    public event Action<DesignControl, RectMm, RectMm>? ControlTransformed;

    public HashSet<DesignControl> SelectedControls { get; } = new();
    public event Action<List<OutlineNode>>? OutlineChanged;
    public event Action<double, double>? PaperSizeChanged;
    // セクションヘッダークリック通知
    public event Action<SectionDefinition>? SectionClicked;
    public double PaperWidthMm { get; set; } = 210;
    public double PaperHeightMm { get; set; } = 297;
    // ==========================
    // Margin (Twip保持)
    // ==========================
    public int TopMarginTwip { get; private set; } = 0;
    public int BottomMarginTwip { get; private set; } = 0;
    public int LeftMarginTwip { get; private set; } = 0;
    public int RightMarginTwip { get; private set; } = 0;
    public double LeftMarginMm => TwipToMm(LeftMarginTwip);
    public double RightMarginMm => TwipToMm(RightMarginTwip);
    public double TopMarginMm => TwipToMm(TopMarginTwip);
    public double BottomMarginMm => TwipToMm(BottomMarginTwip);
    public int PaperSize { get; private set; }
    public string PaperName { get; private set; } = "";
    public int Orientation { get; set; }
    public double GridMm { get; set; } = 1;
    public bool IsLandscape { get; set; } = false;
    private readonly List<SectionDefinition> _sections = new();
    private readonly Dictionary<DesignControl, Control> _adornerMap = new();
    private readonly List<string> _undo = new();
    private const int UndoMax = 30;
    private readonly List<DesignGroup> _groups = new();
    private readonly UndoRedoManager _undoManager = new();
    private bool _updatingPaper;
    private Border? _currentSectionHighlight;
    private const string GridTag = "GRID";

    // =====================
    // Line ID カウンタ
    // =====================
    private int _lineIdCounter = 1;
    public int NextLineId()
    {
        return _lineIdCounter++;
    }
    private enum GripKind
    {
        None,
        BottomRight,
        BottomLeft,
        TopRight,
        TopLeft
    }
    public DesignControl? SelectedControl { get; private set; }
    public bool IsDrawingLine { get; set; }
    private Dictionary<string, object?>? _currentRow;
    private readonly Dictionary<DesignControl, DesignControlView> _viewMap = new();
    private double _pageWidthPx;
    private readonly List<DesignControl> _controls = new();
    // ✅ コンストラクタ
    public DesignerCanvasLogic(Canvas pageCanvas, Border pageBorder)
    {
        _pageCanvas = pageCanvas;
        _pageBorder = pageBorder;
        // ★Designerは左上原点固定
        Canvas.SetLeft(_pageBorder, 0);
        Canvas.SetTop(_pageBorder, 0);
    }
    private double UnitToMm(double valueInTemplateUnit)
    {
        return valueInTemplateUnit / UnitsPerMm;
    }
    //private static double MmToPx(double mm)
    //{
    //    return mm * Dpi / 25.4;
    //}
    // =========================================================
    // ✅ ★追加：用紙サイズ変更を必ずここに集約（イベント発火）
    // =========================================================
    public void SetPaperSize(double widthMm, double heightMm)
    {
        if (_updatingPaper)return;
        if (Math.Abs(PaperWidthMm - widthMm) < 0.0001 &&
            Math.Abs(PaperHeightMm - heightMm) < 0.0001) return;
        try
        {
            _updatingPaper = true;
            PaperWidthMm = widthMm;
            PaperHeightMm = heightMm;
            //ApplyPaperSize();
        }
        finally
        {
            _updatingPaper = false;
        }
    }
    // -----------------------------
    // Public API
    // -----------------------------
    public void Clear()
    {
        _sections.Clear();
        _pageCanvas.Children.Clear();
        _adornerMap.Clear();
        _sourceJsonNode = null;
        _rootKeyName = "";
        _undo.Clear();
    }
    public void SetJson(string path)
    {
        string json = File.ReadAllText(path);
        SetJsonFromString(json);
    }
    private void ForceAllFontsToMSGothic()
    {
        foreach (var sec in _sections)
        {
            foreach (var ctrl in sec.Controls)
            {
                // ✅ Style に font-family があろうが無視する
                ctrl.FontName = "MS Gothic";

                // ✅ Style文字列も上書き
                if (string.IsNullOrEmpty(ctrl.Style))
                {
                    ctrl.Style = "font-family:MS Gothic;";
                }
                else
                {
                    // 既存にfont-familyがあれば置換、なければ追加
                    if (ctrl.Style.Contains("font-family"))
                    {
                        ctrl.Style =
                            System.Text.RegularExpressions.Regex.Replace(
                                ctrl.Style,
                                @"font-family\s*:\s*[^;]+;",
                                "font-family:MS Gothic;"
                            );
                    }
                    else
                    {
                        ctrl.Style += ";font-family:MS Gothic;";
                    }
                }
            }
        }
    }
    public void SetJsonFromString(string json)
    {
        try
        {
            _rootJson = JsonNode.Parse(json);
            if (_rootJson == null)
            {
                StatusHandler?.Invoke("ACR-004: テンプレートファイルの解析に失敗しました。", true);
                return;
            }

            JsonNode reportRoot = DetectRoot(_rootJson);

            // ⑦ RPX2ACR形式: container["Report"] が存在する場合
            if (reportRoot["Report"] != null)
                reportRoot = reportRoot["Report"]!;

            var ps = reportRoot["PageSettings"];
            if (ps == null)
            {
                StatusHandler?.Invoke(
                    "ACR-005: テンプレートファイル（PageSettings)に存在しません。サポートへご連絡ください。",
                    true);
                return;
            }

            LeftMarginTwip = ps["LeftMargin"]?.GetValue<int>() ?? 0;
            RightMarginTwip = ps["RightMargin"]?.GetValue<int>() ?? 0;
            TopMarginTwip = ps["TopMargin"]?.GetValue<int>() ?? 0;
            BottomMarginTwip = ps["BottomMargin"]?.GetValue<int>() ?? 0;
            Orientation = ps["Orientation"]?.GetValue<int>() ?? 1;

            int rawWidth = ps["PaperWidth"]?.GetValue<int>() ?? 11906;
            int rawHeight = ps["PaperHeight"]?.GetValue<int>() ?? 16838;

            Debug.WriteLine($"rawWidth={rawWidth} rawHeight={rawHeight}");
            Debug.WriteLine($"Orientation={Orientation}");

            _unitsPerInch = 1440.0;

            double widthMm  = rawWidth  / _unitsPerInch * 25.4;
            double heightMm = rawHeight / _unitsPerInch * 25.4;

            // JSONのPaperWidth/PaperHeightはすでに表示向きの値
            // Orientationによるswapは不要
            PaperWidthMm  = widthMm;
            PaperHeightMm = heightMm;
            IsLandscape   = (Orientation == 2);

            Debug.WriteLine($"Final PaperWidthMm = {PaperWidthMm}");
            Debug.WriteLine($"Final PaperHeightMm = {PaperHeightMm}");

            LoadSectionsFromJsonString(json);

            PaperSizeChanged?.Invoke(PaperWidthMm, PaperHeightMm);
            StatusHandler?.Invoke("テンプレート読み込み完了", false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("===== ACR LOAD ERROR =====");
            Debug.WriteLine(ex.ToString());   // ⭐これが重要
            Debug.WriteLine("==========================");

            StatusHandler?.Invoke(
                $"ACR-006: テンプレート読込中に例外: {ex.Message}",
                true);
        }
    }
    private void ShowWarning(string message)
    {
        Debug.WriteLine("WARNING: " + message);
    }
    private void DrawGroups()
    {
        foreach (var g in _groups)
        {
            if (g.Controls.Count == 0) continue;
            double left = g.Controls.Min(c => c.LeftMm);
            double top = g.Controls.Min(c => c.TopMm);
            double right = g.Controls.Max(c => c.LeftMm + c.WidthMm);
            double bottom = g.Controls.Max(c => c.TopMm + c.HeightMm);
            var rect = new Rectangle
            {
                Width = UnitConverter.MmToPx(right - left),
                Height = UnitConverter.MmToPx(bottom - top),
                Stroke = Brushes.Blue,
                StrokeDashArray = new AvaloniaList<double> { 4, 4 },
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rect, UnitConverter.MmToPx(left));
            Canvas.SetTop(rect, UnitConverter.MmToPx(top));
            _pageCanvas.Children.Add(rect);
        }
    }
    public void ClearSelection()
    {
        Debug.WriteLine("LOGIC ClearSelection");
        // =========================
        // 選択解除
        // =========================
        SelectedControl = null;
        // =========================
        // adorner 全非表示
        // =========================
        foreach (var ad in _adornerMap.Values)
        {
            ad.IsVisible = false;
        }
    }
    public List<SectionSnapshot> GetSectionsSnapshot()
    {
        return _sections.Select(sec => new SectionSnapshot
        {
            Name = sec.Name,
            Height = sec.HeightMm,
            Controls = sec.Controls.ToList()
        }).ToList();
    }
    public void SelectControl(DesignControl ctrl)
    {
        if (_currentSelected == ctrl) return;
        _currentSelected = ctrl;
        Debug.WriteLine("LOGIC SelectControl = " + ctrl.Name);
        SelectedControl = ctrl;
        foreach (var ad in _adornerMap.Values)
            ad.IsVisible = false;
        if (_adornerMap.TryGetValue(ctrl, out var adorner))
        {
            UpdateAdorner(ctrl);
            if (adorner.Parent is Canvas canvas)
            {
                canvas.Children.Remove(adorner);
                canvas.Children.Add(adorner);
            }
            adorner.IsVisible = true;
        }
        ControlSelected?.Invoke(ctrl);
    }
    // ✅ 複数選択対応版
    public void SelectControl(DesignControl ctrl, bool add)
    {
        if (!add) SelectedControls.Clear();
        SelectedControls.Add(ctrl);
        SelectedControl = ctrl; // 既存互換
    }
    public void PushUndoSnapshot()
    {
        if (_sourceJsonNode == null) return;
        string snap = _sourceJsonNode.ToJsonString();
        if (_undo.Count > 0 && _undo[^1] == snap) return;
        _undo.Add(snap);
        if (_undo.Count > UndoMax) _undo.RemoveAt(0);
    }
    public void Undo()
    {
        if (_undo.Count <= 1) return;
        _undo.RemoveAt(_undo.Count - 1);
        string json = _undo[^1];
        LoadSectionsFromJsonString(json);
    }
    // -----------------------------
    // Page枠サイズ更新
    // -----------------------------
    private void UpdatePageBorderSize()
    {
        double wPx = UnitConverter.MmToPx(PaperWidthMm);
        double hPx = UnitConverter.MmToPx(PaperHeightMm);
        _pageBorder.Width = wPx;
        _pageBorder.Height = hPx;
        _pageCanvas.Width = wPx;
        _pageCanvas.Height = hPx;
    }
    private void FixPageWidth()
    {
        if (_pageCanvas == null || _pageBorder == null) return;
        double maxRight = 0;
        foreach (var child in _pageCanvas.Children)
        {
            if (child is Control c)
            {
                double left = Canvas.GetLeft(c);
                if (double.IsNaN(left)) left = 0;
                // Bounds がまだ 0 のことがあるので、Width も併用
                double w = c.Bounds.Width;
                if (w <= 0 && !double.IsNaN(c.Width) && c.Width > 0) w = c.Width;
                double right = left + w;
                if (right > maxRight) maxRight = right;
            }
        }
        double finalWidth = Math.Max(maxRight, _pageWidthPx);
        _pageCanvas.Width = finalWidth;
        _pageBorder.Width = finalWidth;
    }
    private double PxToMm(double px)
    {
        return px * 25.4 / Dpi;
    }
    private DesignControlView? AddControlToCanvas(Canvas canvas, DesignControl ctrl)
    {
        Debug.WriteLine($"AddControlToCanvas {ctrl.Type}");
        Debug.WriteLine($"DRAW {ctrl.Name} mm=({ctrl.LeftMm},{ctrl.TopMm})");
        Debug.WriteLine($"{ctrl.Name} TopMm={ctrl.TopMm}");
        // =========================
        // ここでマージンを絶対に足さない
        // =========================
        double x = Math.Round(UnitConverter.MmToPx(ctrl.LeftMm));
        double y = Math.Round(UnitConverter.MmToPx(ctrl.TopMm));
        // =========================
        // Line
        // =========================
        if (ctrl.Type.Contains("Line"))
        {
            var lineView = new LineView(ctrl);
            Canvas.SetLeft(lineView, 0);
            Canvas.SetTop(lineView, 0);
            lineView.Width  = canvas.Width;
            lineView.Height = canvas.Height;
            lineView.SetValue(Canvas.ZIndexProperty, 50);  // 他のコントロールより前面に
            // ① LineViewのSelectedをSelectControlに接続
            lineView.Selected += c => SelectControl(c);
            canvas.Children.Add(lineView);
            Debug.WriteLine($"FINAL PX LINE {ctrl.Name} X1={ctrl.X1Mm} Y1={ctrl.Y1Mm}");
            return null;
        }
        // =========================
        // Table
        // =========================
        if (ctrl.Type == "Table" && ctrl.Table != null)
        {
            var tableView = new TableControlView(ctrl.Table);
            Canvas.SetLeft(tableView, x);
            Canvas.SetTop(tableView, y);
            tableView.Width = UnitConverter.MmToPx(ctrl.WidthMm);
            tableView.Height = UnitConverter.MmToPx(ctrl.HeightMm);
            tableView.SetValue(Canvas.ZIndexProperty, ctrl.ZIndex);
            canvas.Children.Add(tableView);
            Debug.WriteLine($"FINAL PX TABLE {ctrl.Name} = ({x},{y})");
            return null;
        }
        // =========================
        // Normal Control
        // =========================
        var view = new DesignControlView(ctrl);
        // ★ 選択
        view.Selected += c => SelectControl(c);
        // ★ 移動・リサイズ完了時
        view.DragFinished += (c, oldRect, newRect) =>
        {
            if (!oldRect.EqualsApprox(newRect))
            {
                // ★ _undoManager ではなく View側へ通知
                ControlTransformed?.Invoke(c, oldRect, newRect);
            }
            ControlSelected?.Invoke(c);
        };

        //view.DragFinished += (c, oldRect, newRect) =>
        //{
        //    if (!oldRect.EqualsApprox(newRect))
        //    {
        //        _undoManager.Execute(
        //            new PropertyChangeCommand(
        //                redo: () =>
        //                {
        //                    c.LeftMm = newRect.Left;
        //                    c.TopMm = newRect.Top;
        //                    c.WidthMm = newRect.Width;
        //                    c.HeightMm = newRect.Height;  // ★ Width→Height に修正
        //                    UpdateControl(c);
        //                },
        //                undo: () =>
        //                {
        //                    c.LeftMm = oldRect.Left;
        //                    c.TopMm = oldRect.Top;
        //                    c.WidthMm = oldRect.Width;
        //                    c.HeightMm = oldRect.Height;
        //                    UpdateControl(c);
        //                }
        //            )
        //        );
        //    }
        //    // ★ ドラッグ完了後にプロパティグリッドを更新
        //    ControlSelected?.Invoke(c);
        //};
        _viewMap[ctrl] = view;
        Canvas.SetLeft(view, x);
        Canvas.SetTop(view, y);
        view.Width = UnitConverter.MmToPx(ctrl.WidthMm);
        view.Height = UnitConverter.MmToPx(ctrl.HeightMm);
        view.SetValue(Canvas.ZIndexProperty, ctrl.ZIndex);
        canvas.Children.Add(view);
        return view;
    }
    private void UpdateAdorner(DesignControl ctrl)
    {
        if (!_adornerMap.TryGetValue(ctrl, out var adorner))
            return;

        double left, top, w, h;

        if (ctrl.Type.Contains("Line"))
        {
            left = UnitConverter.MmToPx(Math.Min(ctrl.X1Mm, ctrl.X2Mm));
            top = UnitConverter.MmToPx(Math.Min(ctrl.Y1Mm, ctrl.Y2Mm));
            w = UnitConverter.MmToPx(Math.Abs(ctrl.X2Mm - ctrl.X1Mm));
            h = UnitConverter.MmToPx(Math.Abs(ctrl.Y2Mm - ctrl.Y1Mm));
        }
        else
        {
            left = UnitConverter.MmToPx(ctrl.LeftMm);
            top = UnitConverter.MmToPx(ctrl.TopMm);
            w = UnitConverter.MmToPx(ctrl.WidthMm);
            h = UnitConverter.MmToPx(ctrl.HeightMm);
        }
        // 小さすぎると点になる対策
        w = Math.Max(6, w);
        h = Math.Max(6, h);
        adorner.Width = w;
        adorner.Height = h;
        Canvas.SetLeft(adorner, left);
        Canvas.SetTop(adorner, top);
        adorner.IsVisible = true;
        Debug.WriteLine($"ADORNER SIZE {ctrl.Name} {w:F1}x{h:F1}");
    }
    // =========================================================
    // グリッドの表示（高速版：Line大量生成→Path1個）
    // =========================================================
    private void DrawGridOverlay(Canvas canvas,
                                 double widthMm,
                                 double heightMm,
                                 double gridMm)
    {
        Debug.WriteLine("DrawGridOverlay gridMm = " + gridMm);
        // ==============================
        // ① 既存グリッド削除
        // ==============================
        for (int i = canvas.Children.Count - 1; i >= 0; i--)
        {
            if (canvas.Children[i] is Line l &&
                Equals(l.Tag, GridTag))
            {
                canvas.Children.RemoveAt(i);
            }
        }
        if (gridMm <= 0)return;
        double stepPx = UnitConverter.MmToPx(gridMm);
        double wPx = UnitConverter.MmToPx(widthMm);
        double hPx = UnitConverter.MmToPx(heightMm);
        // ==============================
        // ② 縦線
        // ==============================
        for (double x = 0; x <= wPx; x += stepPx)
        {
            var line = new Line
            {
                Tag = GridTag,
                StartPoint = new Point(x, 0),
                EndPoint = new Point(x, hPx),
                Stroke = Brushes.Gray,
                StrokeThickness = 0.5,
                Opacity = 0.3,
                IsHitTestVisible = false
            };
            canvas.Children.Insert(0, line);
        }
        // ==============================
        // ③ 横線
        // ==============================
        for (double y = 0; y <= hPx; y += stepPx)
        {
            var line = new Line
            {
                Tag = GridTag,
                StartPoint = new Point(0, y),
                EndPoint = new Point(wPx, y),
                Stroke = Brushes.Gray,
                StrokeThickness = 0.5,
                Opacity = 0.3,
                IsHitTestVisible = false
            };
            canvas.Children.Insert(0, line);
        }
    }
    // =========================================================
    // ✅ units/inch 自動判定
    // =========================================================
    private void DetectUnitsPerInch(double rawPaperWidth, double rawPaperHeight)
    {
        _unitsPerInch = 1440.0;
    }
    // -----------------------------
    // JSON parsing
    // -----------------------------
    private void LoadSectionsFromJsonString(string json)
    {
        // ★追加：ロード時にOutlineCollectionをリセット
        _outlineCollection.Clear();
        var root = JsonNode.Parse(json);
        if (root == null) return;
        JsonNode reportRoot = DetectRoot(root);
        Debug.WriteLine($"★ DetectRoot キー={_rootKeyName}");

        // ⭐ 新JSON対応
        if (reportRoot["Report"] != null)
            reportRoot = reportRoot["Report"]!;

        var sectionsNode = reportRoot["Sections"];

        if (sectionsNode == null)
        {
            Debug.WriteLine("Sections が見つかりません");
            return;
        }

        _sections.Clear();

        // =========================
        // ⭐ 新JSON（配列）
        // =========================
        if (sectionsNode is JsonArray arr)
        {
            foreach (var sec in arr)
            {
                if (sec is JsonObject obj)
                    _sections.Add(ParseSection(obj));
            }
        }
        // =========================
        // ⭐ 旧JSON（Sections -> Section）
        // =========================
        else if (sectionsNode["Section"] is JsonArray oldArr)
        {
            foreach (var sec in oldArr)
            {
                if (sec is JsonObject obj)
                    _sections.Add(ParseSection(obj));
            }
        }
        else if (sectionsNode["Section"] is JsonObject singleObj)
        {
            _sections.Add(ParseSection(singleObj));
        }

        Debug.WriteLine($"Sections loaded = {_sections.Count}");
    }
    private SectionDefinition ParseSection(JsonNode secNode)
    {
        string rawName = secNode["Name"]?.ToString() ?? "Section";

        // ★ セクション名を正規化
        string name = NormalizeSectionName(rawName);

        double rawHeight = secNode["Height"]?.GetValue<double>() ?? 0;
        double heightMm = UnitToMm(rawHeight);

        // ★ Height:0対策
        if (heightMm < 0.1) heightMm = 0.1;

        var section = new SectionDefinition(name, "Detail");
        section.HeightMm = heightMm;
        section.SourceNode = secNode;

        // rowsPerPage（ACR.Detail拡張）
        if (secNode["rowsPerPage"] != null)
            section.RowsPerPage = secNode["rowsPerPage"]?.GetValue<int>() ?? 0;

        var controls = secNode["Control"];
        if (controls is JsonArray arr)
        {
            foreach (var cNode in arr)
            {
                if (cNode != null)
                    section.Controls.Add(ParseControl(cNode));
            }
        }
        else if (controls != null)
        {
            section.Controls.Add(ParseControl(controls));
        }
        return section;
    }
    private static string NormalizeSectionName(string name)
    {
        return name.Trim().ToLowerInvariant() switch
        {
            "pageheader" or "pageheader1" => "PageHeader",
            "pagefooter" or "pagefooter1" => "PageFooter",
            "detail" => "Detail",
            _ => name.Length > 0
                    ? char.ToUpper(name[0]) + name.Substring(1)
                    : name
        };
    }

    // ⭐ここ追加
    private double _rawWidth = 1;
    private double _rawHeight = 1;
    private DesignControl ParseControl(JsonNode cNode)
    {
        // ========= 安全取得 =========
        static double GetD(JsonNode node, string key)
        {
            var v = node[key];
            if (v == null) return 0;

            if (v is JsonValue jv)
            {
                if (jv.TryGetValue<double>(out var d)) return d;
                if (jv.TryGetValue<int>(out var i)) return i;

                var s = jv.ToString();

                if (double.TryParse(
                    s,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var p))
                    return p;
            }

            return 0;
        }

        static int GetI(JsonNode node, string key, int def = 0)
        {
            var v = node[key];
            if (v == null) return def;

            if (v is JsonValue jv)
            {
                if (jv.TryGetValue<int>(out var i)) return i;

                var s = jv.ToString();

                if (s.StartsWith("#"))
                {
                    if (int.TryParse(
                        s.Substring(1),
                        System.Globalization.NumberStyles.HexNumber,
                        null,
                        out var hex))
                        return hex;
                }

                if (int.TryParse(s, out var p)) return p;
            }

            return def;
        }

        // ========= Control生成 =========
        var ctrl = new DesignControl
        {
            SourceNode = cNode,
            Type = cNode["Type"]?.ToString() ?? "",
            Name = cNode["Name"]?.ToString() ?? ""
        };

        // ========= Style =========
        ctrl.Style = cNode["Style"]?.ToString() ?? "";
        ParseStyle(ctrl);

        // ========= 座標（RPX twips対応） =========
        double left = GetD(cNode, "Left");
        double top = GetD(cNode, "Top");
        double width = GetD(cNode, "Width");
        double height = GetD(cNode, "Height");

        // 新JSON fallback
        if (left == 0 && cNode["LeftMm"] != null)
            left = UnitConverter.MmToTwips(GetD(cNode, "LeftMm"));

        if (top == 0 && cNode["TopMm"] != null)
            top = UnitConverter.MmToTwips(GetD(cNode, "TopMm"));

        if (width == 0 && cNode["WidthMm"] != null)
            width = UnitConverter.MmToTwips(GetD(cNode, "WidthMm"));

        if (height == 0 && cNode["HeightMm"] != null)
            height = UnitConverter.MmToTwips(GetD(cNode, "HeightMm"));

        // twips → mm
        ctrl.LeftMm = UnitConverter.TwipsToMm(left);
        ctrl.TopMm = UnitConverter.TwipsToMm(top);
        ctrl.WidthMm = UnitConverter.TwipsToMm(width);
        ctrl.HeightMm = UnitConverter.TwipsToMm(height);

        // ========= 線 =========
        ctrl.LineStyle = GetI(cNode, "LineStyle");

        if (ctrl.Type.Contains("Line"))
        {
            ctrl.X1Mm = UnitConverter.TwipsToMm(GetD(cNode, "X1"));
            ctrl.Y1Mm = UnitConverter.TwipsToMm(GetD(cNode, "Y1"));
            ctrl.X2Mm = UnitConverter.TwipsToMm(GetD(cNode, "X2"));
            ctrl.Y2Mm = UnitConverter.TwipsToMm(GetD(cNode, "Y2"));

            if (ctrl.WidthMm < 1) ctrl.WidthMm = 1;
            if (ctrl.HeightMm < 1) ctrl.HeightMm = 1.5;
        }

        // ========= 色 =========
        ctrl.BackColor = GetI(cNode, "BackColor");
        ctrl.BackStyle = GetI(cNode, "BackStyle", 1);
        ctrl.LineColor = GetI(cNode, "LineColor");
        ctrl.RoundingRadius = GetD(cNode, "RoundingRadius");

        // =========================
        // ⭐ DataField（最重要）
        // =========================
        ctrl.DataField = cNode["DataField"]?.ToString() ?? "";

        // =========================
        // ⭐ Text（固定文字用）
        // =========================
        ctrl.Text = cNode["Text"]?.ToString() ?? "";

        // ========= Shape黒対策 =========
        if (ctrl.Type.Contains("Shape") && cNode["BackColor"] == null)
            ctrl.BackStyle = 0;

        return ctrl;
    }
    private void ParseStyle(DesignControl ctrl)
    {
        if (string.IsNullOrEmpty(ctrl.Style)) return;
        string s = ctrl.Style;
        if (s.Contains("font-family:")) ctrl.FontName = ExtractStyleValue(s, "font-family");
        if (s.Contains("font-size:"))
        {
            string raw = ExtractStyleValue(s, "font-size").Replace("pt", "");
            if (double.TryParse(raw, out var pt)) ctrl.FontSizePt = pt;
        }
        ctrl.Bold = s.Contains("bold");
        if (s.Contains("text-align: center")) ctrl.TextAlign = "center";
        else if (s.Contains("text-align: right")) ctrl.TextAlign = "right";
        else ctrl.TextAlign = "left";
    }
    private string ExtractStyleValue(string style, string key)
    {
        int idx = style.IndexOf(key + ":");
        if (idx < 0) return "";
        int start = idx + key.Length + 1;
        int end = style.IndexOf(";", start);
        if (end < 0) end = style.Length;
        return style.Substring(start, end - start).Trim();
    }
    // =========================================================
    // ✅ OutlineTree生成（Page → Section → Control）
    // =========================================================
    private List<OutlineNode> BuildOutlineTree()
    {
        var pageNode = new OutlineNode
        {
            Name = "Page",
            Type = "Page",
            IsExpanded = true  // ★常に展開
        };
        foreach (var sec in _sections)
        {
            var secNode = new OutlineNode
            {
                Name = sec.Name,
                Type = sec.Name,
                Target = sec,
                IsExpanded = true  // ★常に展開
            };
            foreach (var ctrl in sec.Controls)
            {
                secNode.Children.Add(new OutlineNode
                {
                    Name = string.IsNullOrWhiteSpace(ctrl.Name)
                            ? ctrl.Type
                            : ctrl.Name,
                    Type = ctrl.Type,
                    Target = ctrl
                });
            }
            pageNode.Children.Add(secNode);
        }
        return new List<OutlineNode> { pageNode };
    }
    public void SaveJson(string path)
    {
        // PaperWidth/PaperHeightはすでに表示向きの値のまま保存
        // swapしない（ActiveReports仕様）
        double saveWidthMm  = PaperWidthMm;
        double saveHeightMm = PaperHeightMm;

        // セクションリストをゼロから構築
        var sectionList = new List<Dictionary<string, object>>();
        foreach (var sec in _sections)
        {
            var controlList = new List<Dictionary<string, object>>();
            foreach (var ctrl in sec.Controls)
            {
                var cDict = new Dictionary<string, object>
                {
                    ["Type"]      = ctrl.Type,
                    ["Name"]      = ctrl.Name,
                    ["Left"]      = MmToTwip(ctrl.LeftMm),
                    ["Top"]       = MmToTwip(ctrl.TopMm),
                    ["Width"]     = MmToTwip(ctrl.WidthMm),
                    ["Height"]    = MmToTwip(ctrl.HeightMm),
                    ["Text"]      = ctrl.Text ?? "",
                    ["DataField"] = ctrl.DataField ?? "",
                    ["Style"]     = ctrl.Style ?? "",
                    ["_tag"]      = "Control"
                };
                if (ctrl.Type.Contains("Line"))
                {
                    cDict["X1"] = MmToTwip(ctrl.X1Mm);
                    cDict["Y1"] = MmToTwip(ctrl.Y1Mm);
                    cDict["X2"] = MmToTwip(ctrl.X2Mm);
                    cDict["Y2"] = MmToTwip(ctrl.Y2Mm);
                }
                if (ctrl.Type.Contains("Shape"))
                {
                    cDict["BackColor"] = ctrl.BackColor;
                    cDict["BackStyle"] = ctrl.BackStyle;
                    cDict["LineColor"] = ctrl.LineColor;
                }
                controlList.Add(cDict);
            }

            var secDict = new Dictionary<string, object>
            {
                ["Type"]      = $"ACR.{sec.Name}",
                ["Name"]      = sec.Name,
                ["Height"]    = MmToTwip(sec.HeightMm),
                ["BackColor"] = 16777215,
                ["Control"]   = controlList
            };
            // ACR.Detail 拡張：rowsPerPage
            if (sec.Name == "Detail")
                secDict["rowsPerPage"] = sec.RowsPerPage;
            sectionList.Add(secDict);
        }

        var root = new Dictionary<string, object>
        {
            [string.IsNullOrEmpty(_rootKeyName) ? "AcrReport" : _rootKeyName] =
                new Dictionary<string, object>
                {
                    ["PageSettings"] = new Dictionary<string, object>
                    {
                        ["LeftMargin"]   = LeftMarginTwip,
                        ["RightMargin"]  = RightMarginTwip,
                        ["TopMargin"]    = TopMarginTwip,
                        ["BottomMargin"] = BottomMarginTwip,
                        ["PaperSize"]    = PaperSize,
                        ["PaperName"]    = PaperName,
                        ["PaperWidth"]   = MmToTwip(saveWidthMm),
                        ["PaperHeight"]  = MmToTwip(saveHeightMm),
                        ["Orientation"]  = Orientation
                    },
                    ["Sections"] = new Dictionary<string, object>
                    {
                        ["Section"] = sectionList
                    }
                }
        };

        File.WriteAllText(path,
            JsonSerializer.Serialize(root,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    // =======================================
    // ✅ 新規帳票（最小テンプレート生成）
    // =======================================
    public void NewReport()
    {
        // =========================
        // A4 横（mm）
        // =========================
        PaperWidthMm = 297;
        PaperHeightMm = 210;
        Orientation = 2; // Landscape
        // =========================
        // マージン 1cm（10mm）
        // =========================
        int marginTwip = (int)Math.Round(UnitConverter.MmToTwips(10));
        LeftMarginTwip = marginTwip;
        RightMarginTwip = marginTwip;
        TopMarginTwip = marginTwip;
        BottomMarginTwip = marginTwip;
        // =========================
        // Grid 初期値
        // =========================
        GridMm = 10;
        // =========================
        // JSONルート構造を生成
        // =========================
        string json = """
        {
          "AcrReport": {
            "PageSettings": {
              "LeftMargin": 567,
              "RightMargin": 567,
              "TopMargin": 567,
              "BottomMargin": 567,
              "PaperSize": 9,
              "PaperWidth": 16838,
              "PaperHeight": 11906,
              "PaperName": "A4",
              "Orientation": 2
            },
            "Sections": {
              "Section": [
                { "Type": "ACR.PageHeader", "Name": "PageHeader", "Height": 360, "BackColor": 16777215, "Control": [] },
                { "Type": "ACR.Detail",     "Name": "Detail",     "Height": 2880, "rowsPerPage": 0, "BackColor": 16777215, "Control": [] },
                { "Type": "ACR.PageFooter", "Name": "PageFooter", "Height": 360, "BackColor": 16777215, "Control": [] }
              ]
            }
          }
        }
        """;
        SetJsonFromString(json);
    }

    public void UpdateSectionHeight(string sectionName, double heightMm)
    {
        var sec = _sections.FirstOrDefault(s => s.Name == sectionName);
        if (sec != null)
        {
            sec.HeightMm = heightMm;
        }
    }
    public void UpdateControlView(DesignControl ctrl)
    {
        if (!_viewMap.TryGetValue(ctrl, out var view)) return;
        view.ApplyPosition();
        UpdateAdorner(ctrl);
    }
    private JsonNode DetectRoot(JsonNode node)
    {
        string[] candidates =
        {
            "ACR",
            "AcrossReport",
            "AcReport",
            "AcrReport",
            "ArcReport"
        };

        foreach (var key in candidates)
        {
            if (node[key] == null)
                continue;

            _rootKeyName = key;

            JsonNode container = node[key]!;

            // ⑦ RPX2ACR形式: ACR -> { Meta:{}, Report:{} }
            if (container["Report"] != null)
                return container;   // Report はここで返し、呼び元で["Report"]を取得

            if (container is JsonObject) return container;
            if (container is JsonArray arr && arr.Count > 0) return arr[0]!;

            throw new Exception($"{key} が空です");
        }

        throw new Exception("Report Rootが見つかりません");
    }
    private static double TwipToMm(int twip)
    {
        return twip / 56.6929;
    }

    private static int MmToTwip(double mm)
    {
        return (int)Math.Round(mm * 56.6929);
    }

    public void MoveControlToFront(DesignControl ctrl)
    {
        var sec = _sections.FirstOrDefault(s => s.Controls.Contains(ctrl));
        if (sec == null) return;

        sec.Controls.Remove(ctrl);
        sec.Controls.Add(ctrl);   // 最後 = 最前面
    }

    public void MoveControlToBack(DesignControl ctrl)
    {
        var sec = _sections.FirstOrDefault(s => s.Controls.Contains(ctrl));
        if (sec == null) return;
        sec.Controls.Remove(ctrl);
        sec.Controls.Insert(0, ctrl); // 先頭 = 背面
    }

    public void AddGroupForControl(DesignControl ctrl)
    {
        // すでにどこかの Group に入っていれば何もしない
        if (_groups.Any(g => g.Controls.Contains(ctrl))) return;
        var group = new DesignGroup
        {
            Name = $"Group{_groups.Count + 1}"
        };
        group.Controls.Add(ctrl);
        _groups.Add(group);

        Debug.WriteLine($"Group created: {group.Name}, Control={ctrl.Name}");
    }
    // 公開メソッド
    public void AddGroup(IEnumerable<DesignControl> controls)
    {
        var list = controls.ToList();
        if (list.Count == 0) return;
        var group = new DesignGroup
        {
            Name = $"Group{_groups.Count + 1}"
        };
        foreach (var c in list)
            group.Controls.Add(c);
        _groups.Add(group);
        Debug.WriteLine($"Group added: {group.Name} ({group.Controls.Count})");
    }
    // ======================================================
    // ✅ レポート Section グループ追加
    // ======================================================
    public void AddGroupSection()
    {
        int count = _sections.Count(s => s.Name.StartsWith("GroupHeader"));
        int next = count + 1;
        string headerName = $"GroupHeader{next}";
        string footerName = $"GroupFooter{next}";
        double headerMm = UnitToMm(400);
        double footerMm = UnitToMm(400);
        var header = new SectionDefinition(headerName, "PageHeader");
        header.HeightMm = headerMm;
        var footer = new SectionDefinition(footerName, "PageFooter");
        footer.HeightMm = footerMm;
        header.SourceNode = new JsonObject
        {
            ["Type"] = "ACR.GroupHeader",
            ["Name"] = headerName,
            ["Height"] = 400
        };
        footer.SourceNode = new JsonObject
        {
            ["Type"] = "ACR.GroupFooter",
            ["Name"] = footerName,
            ["Height"] = 400
        };
        // ============================
        // 挿入位置決定
        // ============================
        // ① 最後の GroupFooter を探す
        // 最初のGroupHeaderのインデックスを探す（なければDetailの位置）
        int firstGroupHeader = _sections.FindIndex(s => s.Name.StartsWith("GroupHeader"));
        int detailIndex = _sections.FindIndex(s => s.Name == "Detail");
        int lastGroupFooter = _sections.FindLastIndex(s => s.Name.StartsWith("GroupFooter"));

        if (firstGroupHeader >= 0)
        {
            // 既存グループの外側に追加
            // Header → 最初のGroupHeaderの上
            // Footer → 最後のGroupFooterの下
            _sections.Insert(firstGroupHeader, header);
            // headerを挿入したのでインデックスが1ずれる
            int newLastFooter = _sections.FindLastIndex(s => s.Name.StartsWith("GroupFooter"));
            _sections.Insert(newLastFooter + 1, footer);
        }
        else if (detailIndex >= 0)
        {
            _sections.Insert(detailIndex, header);
            _sections.Insert(detailIndex + 2, footer);
        }
        else
        {
            _sections.Add(header);
            _sections.Add(footer);
        }
        PushUndoSnapshot();
    }
    private int SectionOrder(SectionDefinition s)
    {
        if (s.Name.StartsWith("PageHeader")) return 10;
        if (s.Name.StartsWith("GroupHeader")) return 20;
        if (s.Name.StartsWith("Detail")) return 30;
        if (s.Name.StartsWith("GroupFooter")) return 40;
        if (s.Name.StartsWith("PageFooter")) return 50;
        return 99;
    }
    public void SaveAcr(string path)
    {
        // -------------------------
        // temp フォルダ作成
        // -------------------------
        string tempDir =
            System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "acr_" + Guid.NewGuid());

        System.IO.Directory.CreateDirectory(tempDir);

        // -------------------------
        // report.json 保存
        // -------------------------
        string jsonPath =
            System.IO.Path.Combine(tempDir, "report.json");

        SaveJson(jsonPath);

        // -------------------------
        // version.json 保存
        // -------------------------
        var versionInfo = new
        {
            format = "ACR",
            formatVersion = 1,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string versionPath =
            System.IO.Path.Combine(tempDir, "version.json");

        System.IO.File.WriteAllText(
            versionPath,
            System.Text.Json.JsonSerializer.Serialize(
                versionInfo,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                }
            ));

        // -------------------------
        // ZIP → .acr
        // -------------------------
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);

        System.IO.Compression.ZipFile
            .CreateFromDirectory(tempDir, path);

        // -------------------------
        // ★検証用：json横出し
        // -------------------------
        string debugJson =
            System.IO.Path.ChangeExtension(path, ".json");

        System.IO.File.Copy(jsonPath, debugJson, true);

        // ↑ リリース時コメントアウト

        // -------------------------
        // temp削除
        // -------------------------
        System.IO.Directory.Delete(tempDir, true);
    }
    public void LoadAcr(string path)
    {
        string tempDir =
            System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "acr_" + Guid.NewGuid());

        System.IO.Directory.CreateDirectory(tempDir);

        // =========================
        // ZIP展開
        // =========================
        System.IO.Compression.ZipFile
            .ExtractToDirectory(path, tempDir);

        // =========================
        // report.json 読込
        // =========================
        string jsonPath =
            System.IO.Path.Combine(tempDir, "report.json");

        if (!System.IO.File.Exists(jsonPath))
            throw new Exception("report.json が見つかりません");

        string json =
            System.IO.File.ReadAllText(jsonPath);

        SetJsonFromString(json);

        // =========================
        // temp削除
        // =========================
        System.IO.Directory.Delete(tempDir, true);
    }

    // ======================================================
    // ✅ ツールからコントロール生成
    // ======================================================
    // =========================================================
    // ✅ 最後に追加されたコントロールを返す（Drop後の選択用）
    // =========================================================
    private DesignControl? _lastAddedControl;
    public DesignControl? GetLastAddedControl() => _lastAddedControl;
    public void CreateControlFromTool(string tool, double xPx, double yPx)
    {
        Debug.WriteLine($"CREATE TOOL {tool} at {xPx},{yPx}");

        // HeaderPxを考慮したセクション解決
        var (sec, xMm, yMmInSection) = ResolveCanvasPxToSection(xPx, yPx);

        if (sec == null)
        {
            Debug.WriteLine("NO SECTION FOUND");
            return;
        }

        Debug.WriteLine($"  → sec={sec.Name} x={xMm:F2}mm y={yMmInSection:F2}mm");

        DesignControl ctrl;

        // =========================
        // ツール別生成
        // =========================
        if (tool == "Label")
        {
            ctrl = new DesignControl
            {
                Type = "Label",
                Name = GenerateNextName("Label"),
                LeftMm = xMm,
                TopMm = yMmInSection,
                WidthMm = 20,
                HeightMm = 10,
                Text = "Label",
                FontName = "",          // OSデフォルト
                FontFamily = "",        // OSデフォルト
                FontSize = 10,
                FontSizePt = 10
            };
        }
        else if (tool == "TextBox")
        {
            ctrl = new DesignControl
            {
                Type = "TextBox",
                Name = GenerateNextName("TextBox"),
                LeftMm = xMm,
                TopMm = yMmInSection,
                WidthMm = 20,
                HeightMm = 10,
                Text = "",
                FontName = "",          // OSデフォルト
                FontFamily = "",        // OSデフォルト
                FontSize = 10,
                FontSizePt = 10
            };
        }
        else if (tool == "Line")
        {
            ctrl = new DesignControl
            {
                Type = "Line",
                Name = GenerateNextName("Line"),
                X1Mm = xMm,
                Y1Mm = yMmInSection,
                X2Mm = xMm + 20,
                Y2Mm = yMmInSection
            };

            sec.Controls.Add(ctrl);
            _lastAddedControl = ctrl;
            Debug.WriteLine($"LINE ADDED to {sec.Name}");
            return;
        }
        else if (tool == "Shape")
        {
            ctrl = new DesignControl
            {
                Type = "Shape",
                Name = GenerateNextName("Shape"),
                LeftMm = xMm,
                TopMm = yMmInSection,
                WidthMm = 20,
                HeightMm = 10,

                // ⭐ AR互換デフォルト
                BackColor = 0xDCDCDC,
                LineColor = 0xFFFFFF,
                BackStyle = 1
            };
        }
        else
        {
            Debug.WriteLine("UNKNOWN TOOL");
            return;
        }
 
        // =========================
        // セクションへ追加
        // =========================
        sec.Controls.Add(ctrl);
        _lastAddedControl = ctrl;

        Debug.WriteLine($"CONTROL ADDED to {sec.Name}");

        // =========================
        // 再描画
        // =========================
        //_outlineDirty = true;   // ←追加
    }

    // =========================================================
    // PageCanvas上のpx座標から、セクションとセクション内mm座標を解決
    // 各バンドは HeaderPx(22px) + bodyPx の構造
    // =========================================================
    private const double HeaderPx = 22.0;

    private SectionDefinition? FindSectionByPageMm(double pageYmm)
    {
        // pageYmm は PageCanvas上のpxをmmに変換した値
        // バンドは HeaderPx + bodyPx の構造なので px で計算してからmmに戻す
        double pageYpx = pageYmm * Dpi / 25.4;
        double accPx = 0;
        foreach (var s in _sections)
        {
            double bodyPx = Math.Round(UnitConverter.MmToPx(s.HeightMm));
            double bandPx = HeaderPx + bodyPx;
            // ヘッダー帯内（22px）はセクション本体ではないが、
            // ドロップ先として直近のセクションを返す
            if (pageYpx >= accPx && pageYpx < accPx + bandPx)
                return s;
            accPx += bandPx;
        }
        return null;
    }

    // PageCanvas上のpx座標をセクション内ローカルmm座標に変換
    public (SectionDefinition? sec, double localXmm, double localYmm)
        ResolveCanvasPxToSection(double xPx, double yPx)
    {
        double accPx = 0;
        foreach (var s in _sections)
        {
            double bodyPx = Math.Round(UnitConverter.MmToPx(s.HeightMm));
            double bandPx = HeaderPx + bodyPx;
            if (yPx >= accPx && yPx < accPx + bandPx)
            {
                // ヘッダー帯を除いたボディ内のローカルY
                double localYpx = yPx - accPx - HeaderPx;
                if (localYpx < 0) localYpx = 0;
                double localYmm = SnapMm(localYpx * 25.4 / Dpi);
                // セクション内に収める
                localYmm = Math.Max(0, Math.Min(localYmm, s.HeightMm - 1));
                double localXmm = SnapMm(xPx * 25.4 / Dpi);
                return (s, localXmm, localYmm);
            }
            accPx += bandPx;
        }
        return (null, 0, 0);
    }

    private double SnapMm(double v)
    {
        if (GridMm <= 0) return v;
        return Math.Round(v / GridMm) * GridMm;
    }
    // View側から呼べるpublic版
    public double SnapToGrid(double mm) => SnapMm(mm);
    // Section特定（ページ基準Y）    
    public SectionDefinition? FindSectionByMmY(double yMm)
    {
        Debug.WriteLine($"[HitTest] PageYmm = {yMm:F2}");
        double pos = 0;
        foreach (var sec in _sections)
        {
            double start = pos;
            double end = pos + sec.HeightMm;
            if (yMm >= start && yMm < end)
            {
                double local = yMm - start;
                if (local < 0) local = 0;
                Debug.WriteLine(
                    $"  Section = {sec.Name}  " +
                    $"Range = {start:F2}-{end:F2}  " +
                    $"LocalY = {local:F2}");
                return sec;
            }
            pos += sec.HeightMm;
        }
        Debug.WriteLine("  Section = NONE");
        return null;
    }
    // =========================
    // Section帯ハイライト
    // =========================
    // TreeでSection選択時に呼ぶ
    public void HighlightSection(SectionDefinition section)
    {
        ClearSectionHighlight();

        foreach (var child in _pageCanvas.Children)
        {
            if (child is Border b &&
                b.Tag is SectionDefinition s &&
                s == section)
            {
                // ✅ 透明ではなく「薄いハイライト色」
                b.Background = new SolidColorBrush(Color.FromArgb(18, 255, 200, 0)); // 薄い黄
                _currentSectionHighlight = b;
                break;
            }
        }
    }
    public void ClearSectionHighlight()
    {
        if (_currentSectionHighlight != null)
        {
            _currentSectionHighlight.Background = Brushes.Transparent;
            _currentSectionHighlight = null;
        }
    }

    public void AddControlToTableCell(TableControlView tableView,int row,int col,DesignControl ctrl)
    {
        var canvas = tableView.GetCellCanvas(row, col);
        if (canvas == null) return;
        var view = new DesignControlView(ctrl);

        // ★ここを追加する
        view.AddHandler(InputElement.PointerPressedEvent,
            (s, e) =>
            {
                e.Handled = true;
                SelectControl(ctrl);
            },
            RoutingStrategies.Tunnel);

        canvas.Children.Add(view);
    }
    //Section開始Y(mm)を返す関数
    public double GetSectionStartMm(SectionDefinition target)
    {
        double y = 0;
        foreach (var sec in _sections)
        {
            if (sec == target) return y;
            y += sec.HeightMm;
        }
        return 0;
    }
    public string GenerateNextName(string type)
    {
        int max = 0;
        foreach (var sec in _sections)
            foreach (var c in sec.Controls)
            {
                if (!string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(c.Name))
                    continue;
                var digits = new string(
                    c.Name.SkipWhile(ch => !char.IsDigit(ch)).ToArray());
                if (int.TryParse(digits, out int n))
                    if (n > max) max = n;
            }
        return type + (max + 1);
    }
    private void FlushModelToSourceNodes()
    {
        foreach (var sec in _sections)
        {
            if (sec.SourceNode is not JsonObject secNode) continue;
            var controlsArray = secNode["Control"] as JsonArray;
            if (controlsArray == null) continue;
            for (int i = 0; i < sec.Controls.Count && i < controlsArray.Count; i++)
            {
                var ctrl = sec.Controls[i];
                if (controlsArray[i] is not JsonObject node) continue;
                node["DataField"] = ctrl.DataField ?? "";
                node["Text"] = ctrl.Text ?? "";
                node["Left"] = MmToTwip(ctrl.LeftMm);
                node["Top"] = MmToTwip(ctrl.TopMm);
                node["Width"] = MmToTwip(ctrl.WidthMm);
                node["Height"] = MmToTwip(ctrl.HeightMm);
            }
        }
    }
    private static string ResolveLogType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return "Unknown";

        // ACR系はそのまま出す
        if (type.StartsWith("ACR.", StringComparison.OrdinalIgnoreCase))
            return type;

        // それ以外も Unknown に落とさない
        return type;
    }
    // =====================================================
    // 用紙サイズテーブル（mm）
    // =====================================================
    private static readonly Dictionary<string, (double w, double h)> _paperTable
        = new()
        {
            ["A3"] = (297, 420),
            ["A4"] = (210, 297),
            ["B4"] = (257, 364),
            ["B5"] = (182, 257),
        };

    // =====================================================
    // 名前で用紙サイズ適用
    // =====================================================
    public void ApplyPaperByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (!_paperTable.TryGetValue(name, out var s))
            return;

        SetPaperSize(s.w, s.h);
    }
    // =============================
    // View マップAPI
    // =============================
    public bool TryGetView(DesignControl model, out DesignControlView view)
    {
        return _viewMap.TryGetValue(model, out view!);
    }
    public void RegisterView(DesignControl model, DesignControlView view)
    {
        _viewMap[model] = view;
    }
    public void RemoveView(DesignControl model)
    {
        _viewMap.Remove(model);
    }
    public void DeleteControl(DesignControl control)
    {
        if (control == null)return;

        var sec = FindOwnerSection(control);
        if (sec == null) return;
        sec.Controls.Remove(control);
        Render();   // ← あなたの正式な再描画関数
    }
    public void RestoreControl(SectionDefinition sec, int index, DesignControl control)
    {
        if (sec == null || control == null)return;
        if (index < 0 || index > sec.Controls.Count)
            sec.Controls.Add(control);
        else
            sec.Controls.Insert(index, control);
        Render();
    }
    public void SetMarginsMm(double top,double bottom,double left,double right)
    {
        TopMarginTwip = (int)Math.Round(UnitConverter.MmToTwips(top));
        BottomMarginTwip = (int)Math.Round(UnitConverter.MmToTwips(bottom));
        LeftMarginTwip = (int)Math.Round(UnitConverter.MmToTwips(left));
        RightMarginTwip = (int)Math.Round(UnitConverter.MmToTwips(right));
    }
    public SectionDefinition? FindOwnerSection(DesignControl ctrl)
    {
        foreach (var sec in _sections) // ←あなたの実装に合わせて変えてOK
        {
            if (sec.Controls.Contains(ctrl))
                return sec;
        }
        return null;
    }
    public int GetControlIndex(SectionDefinition sec, DesignControl ctrl)
    {
        return sec.Controls.IndexOf(ctrl);
    }
    public void InsertControl(SectionDefinition sec, int index, DesignControl ctrl)
    {
        if (index < 0 || index > sec.Controls.Count)
            sec.Controls.Add(ctrl);
        else
            sec.Controls.Insert(index, ctrl);
        Render(); // 既に持ってる描画更新
    }
    // =========================================
    //  Render
    // =========================================
    public void Render()
    {
        Debug.WriteLine("Render logic hash = " + this.GetHashCode());
        ApplyPaperSize();
        _pageCanvas.Children.Clear();
        ClearSelection();
        double yPx = 0.0;
        foreach (var sec in _sections)
        {
            double bodyPx = Math.Round(UnitConverter.MmToPx(sec.HeightMm));
            double bandHeightPx = HeaderPx + bodyPx;
            Control band = CreateBand(sec);
            band.Width = _pageCanvas.Width;
            band.Height = bandHeightPx;
            Canvas.SetLeft(band, 0.0);
            Canvas.SetTop(band, yPx);
            _pageCanvas.Children.Add(band);
            yPx += bandHeightPx;
        }
        _pageCanvas.Height = yPx;
        _pageBorder.Height = yPx;
        Canvas.SetLeft(_pageBorder, 0);
        Canvas.SetTop(_pageBorder, 0);
        UpdateOutlineTree();
        OutlineChanged?.Invoke(new List<OutlineNode>(_outlineCollection));
    }

    private Control CreateBand(SectionDefinition sec)
    {
        double printableWidthMm = PaperWidthMm - LeftMarginMm - RightMarginMm;
        double printableWidthPx = UnitConverter.MmToPx(printableWidthMm);

        double bodyPx = Math.Round(UnitConverter.MmToPx(sec.HeightMm));
        double bandHeightPx = HeaderPx + bodyPx;

        var bandRoot = new Border
        {
            Width = printableWidthPx,
            Height = bandHeightPx,
            BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            BorderThickness = new Thickness(1.2),
            Background = Brushes.Transparent,
            IsHitTestVisible = true,  // 明示的にtrue
            Tag = sec
        };

        Canvas.SetLeft(bandRoot, UnitConverter.MmToPx(LeftMarginMm));

        var grid = new Grid { Width = printableWidthPx };

        grid.RowDefinitions.Add(new RowDefinition(new GridLength(HeaderPx)));
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(bodyPx)));

        var header = new Border
        {
            Width = printableWidthPx,
            Height = HeaderPx,
            Background = new SolidColorBrush(Color.FromArgb(45, 100, 130, 170)),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = new TextBlock
            {
                Text = $"{sec.Name}  (H={sec.HeightMm:0.#}mm)",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(6, 2)
            }
        };
        // ヘッダークリック → SectionClicked イベント発火
        var capturedSec = sec;
        header.PointerPressed += (s, e) =>
        {
            SectionClicked?.Invoke(capturedSec);
            e.Handled = true;
        };

        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var canvas = new Canvas
        {
            Width = printableWidthPx,
            Height = bodyPx,
            Background = Brushes.Transparent,
            ClipToBounds = true,
            IsHitTestVisible = true  // ★明示的にtrue
        };
        
        Grid.SetRow(canvas, 1);
        // =========================
        // ⭐ここ追加（Grid表示）
        // =========================
        if (GridMm > 0)
        {
            DrawGridOverlay(canvas, printableWidthMm, sec.HeightMm, GridMm);
        }
        // =========================
        // ★ 1回だけ描画
        // =========================
        foreach (var ctrl in sec.Controls)
        {
            var view = AddControlToCanvas(canvas, ctrl);
            if (view != null)
            {
                view.GridMm = GridMm;  // ★null確認後に設定
                _controlViewMap[ctrl] = view;
            }
        }

        grid.Children.Add(canvas);
        bandRoot.Child = grid;

        return bandRoot;
    }
    private void BindSectionData(SectionDefinition section, Dictionary<string, object> row)
    {
        foreach (var ctrl in section.Controls)
        {
            if (string.IsNullOrEmpty(ctrl.DataField))
                continue;

            // =========================
            // Model更新
            // =========================
            if (row.TryGetValue(ctrl.DataField, out var v))
                ctrl.Text = v?.ToString() ?? "";
            else
                ctrl.Text = "";

            Debug.WriteLine($"CTRL={ctrl.Name} DF={ctrl.DataField} TEXT={ctrl.Text}");

            // =========================
            // View更新（←ここ重要）
            // =========================
            if (_controlViewMap.TryGetValue(ctrl, out var view))
            {
                view.UpdateText();
            }
        }
    }
    public void ApplyPaperSize()
    {
        double printableWidthMm = PaperWidthMm - LeftMarginMm - RightMarginMm;
        double printableHeightMm = PaperHeightMm - TopMarginMm - BottomMarginMm;
        double pageWpx = UnitConverter.MmToPx(printableWidthMm);
        double pageHpx = UnitConverter.MmToPx(printableHeightMm);
        _pageBorder.Width = pageWpx;
        _pageBorder.Height = pageHpx;
        _pageCanvas.Width = pageWpx;
        _pageCanvas.Height = pageHpx;
    }
    public void UpdateControl(DesignControl ctrl)
    {
        if (!_viewMap.TryGetValue(ctrl, out var view)) return;
        view.RefreshFromModel();
    }
    private void UpdateOutlineTree()
    {
        // Pageノードを取得または作成
        var pageNode = _outlineCollection.FirstOrDefault(n => n.Type == "Page");
        if (pageNode == null)
        {
            pageNode = new OutlineNode
            {
                Name = "Page",
                Type = "Page",
                IsExpanded = true
            };
            _outlineCollection.Add(pageNode);
        }

        // セクションを同期（追加のみ）
        foreach (var sec in _sections)
        {
            var secNode = pageNode.Children
                .FirstOrDefault(n => ReferenceEquals(n.Target, sec));

            if (secNode == null)
            {
                secNode = new OutlineNode
                {
                    Name = sec.Name,
                    Type = sec.Name,
                    Target = sec,
                    IsExpanded = true
                };
                pageNode.Children.Add(secNode);
            }

            // コントロールを同期（追加のみ）
            foreach (var ctrl in sec.Controls)
            {
                if (!secNode.Children.Any(n => ReferenceEquals(n.Target, ctrl)))
                {
                    secNode.Children.Add(new OutlineNode
                    {
                        Name = string.IsNullOrWhiteSpace(ctrl.Name)
                                ? ctrl.Type
                                : ctrl.Name,
                        Type = ctrl.Type,
                        Target = ctrl
                    });
                }
            }
        }
    }
}
