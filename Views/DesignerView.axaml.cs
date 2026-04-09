using AcrossReportDesigner.Designer;
using AcrossReportDesigner.Models;
using AcrossReportDesigner.Services;
using AcrossReportDesigner.UndoRedo;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Views;

public partial class DesignerView : UserControl
{
    private DesignerCanvasLogic? _logic;
    // ✅ 追加：編集可能状態フラグ
    private bool _pageReady = false;
    private bool _resizing = false;
    private Point _resizeStart;
    private double _startW;
    private double _startH;
    //private bool _isNewMode = false;
    // Tree同期フラグ
    //private bool _syncingTree = false;
    // =========================
    // Section帯ハイライト管理
    // =========================
    private Border? _currentSectionHighlight;
    private readonly Dictionary<SectionDefinition, Border> _sectionBandMap = new();
    private readonly ObservableCollection<OutlineNode> _outlineRoots = new();
    private Border? _currentSectionBand;
    // 選択中Control
    private bool _editingPaperSize;
    // Pan用
    private Point _panStart;
    private Vector _scrollStart;
    private bool _panning;
    private bool _rightDown;
    // ✅ 選択中Section（追加）
    private SectionSnapshot? _currentSection;
    private bool _pageSelected = false;
    private bool _syncingSelection = false;
    public string Name { get; set; } = "Group";
    public List<DesignControl> Controls { get; } = new();
    private readonly UndoRedoManager _undo = new();
    private bool _drawingLine;
    private Point _lineStartPx;
    private Avalonia.Controls.Shapes.Line? _previewLine;
    private List<string> _fontNames = new();
    private bool _fromTree = false;
    // =========================
    // ツール
    // =========================
    enum ToolType
    {
        None,
        Label,
        TextBox,
        Line,
        Shape
    }
    // 用紙向き
    private bool _isLandscape = false;
    // 用紙サイズ（mm）
    private const double A4WidthMm = 210;
    private const double A4HeightMm = 297;
    private const double B4WidthMm = 257;
    private const double B4HeightMm = 364;
    //初期はツールを選ばない
    ToolType _currentTool = ToolType.None;
    private const double HandleSize = 8;
    private bool _resizeRight = false;
    private bool _resizeBottom = false;
    public string Editor { get; set; } = "textbox";
    private Rectangle? _selectionRect;
    private Point _selectStart;
    private bool _selecting;
    private bool _selectionChanging = false;
    private bool _isLoading = false;
    private static double PxToMm(double px)
    {
        return px * 25.4 / 96.0;
    }
    private bool Near(double a, double b)
    {
        return Math.Abs(a - b) < 1.0;
    }
    private enum ResizeHandleType
    {
        None,
        Left,
        Top,
        Right,
        Bottom,
        TopLeft,
        BottomLeft,
        BottomRight
    }
    private SelectionManager? _selection;
    private ZoomController? _zoomController;
    private ContextMenuController? _contextMenu;
    public ObservableCollection<PropertyRow> PropertyRows { get; }
                            = new ObservableCollection<PropertyRow>();
    public DesignerView()
    {
        InitializeComponent();
        InitBorderResize();
        DataContext = this;
        _logic = new DesignerCanvasLogic(PageCanvas, PageBorder);
        _logic.ControlSelected += OnControlSelected;
        _logic.ControlTransformed += OnControlTransformed; // ★追加
        Tree.ItemsSource = _logic.OutlineCollection;
        _logic.OutlineChanged += roots =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                ExpandAllNodes(_logic.OutlineCollection);
            }, DispatcherPriority.Background);
        };
        _logic.SectionClicked += sec =>
        {
            ShowSectionProperties(sec);
            // TreeViewでも該当セクションノードを選択
            if (Tree.ItemsSource is IEnumerable<OutlineNode> roots)
            {
                foreach (var r in roots)
                {
                    var node = FindNodeByTarget(r, sec);
                    if (node != null)
                    {
                        _selectionChanging = true;
                        Tree.SelectedItem = node;
                        _selectionChanging = false;
                        break;
                    }
                }
            }
        };
        NewButton.Click += New_Click;
        ClearButton.Click += Clear_Click;
        LoadButton.Click += Load_Click;
        SaveButton.Click += Save_Click;
        PortraitButton.Click += Portrait_Click;
        LandscapeButton.Click += Landscape_Click;
        PaperSizeCombo.SelectionChanged += PaperSizeCombo_SelectionChanged;
        PageCanvas.PointerPressed += PageCanvas_BackgroundPressed;
        PageCanvas.PointerPressed += PageCanvas_PointerPressed;
        PageCanvas.ContextRequested += PageCanvas_ContextRequested;
        // =====================
        // ツールボタン登録
        // =====================
        WireToolButton(ToolLabelButton,  ToolLabel_Click);
        WireToolButton(ToolTextButton,   ToolText_Click);
        WireToolButton(ToolLineButton,   ToolLine_Click);
        WireToolButton(ToolShapeButton,  ToolShape_Click);
        // =====================
        // ポインターキャプチャ方式：UserControl全体で追跡
        // =====================
        this.PointerMoved    += UserControl_PointerMoved;
        this.PointerReleased += UserControl_PointerReleased;
        GridCombo.SelectionChanged += GridCombo_SelectionChanged;
        _logic.StatusHandler = (message, isError) =>
        {
            StatusText.Text = message;
            StatusText.Foreground = isError
                ? Brushes.Red
                : Brushes.Black;
        };
        // =====================
        // セクション初期化
        // =====================
        _selection = new SelectionManager(PageCanvas);
        _selection.SelectionChanged += ctrl =>
        {
            if (ctrl != null)
                _logic?.SelectControl(ctrl);
        };
        // =====================
        // ズーム初期化
        // =====================
        _zoomController = new ZoomController(
            PageBorder,
            MainScroll,
            ZoomText);
        MainScroll.AddHandler(
            PointerWheelChangedEvent,
            (s, e) => _zoomController?.HandleWheel(e),
            RoutingStrategies.Tunnel);
        // =====================
        // コンテキスト初期化
        // =====================
        _contextMenu = new ContextMenuController(
            PageCanvas,
            _logic!,
            _selection!,
            _undo);
        // =====================
        // ★ 起動初期状態
        // =====================
        this.KeyDown += OnKeyDown;
        _pageReady = false;
        UpdateUIEnabled();
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _undo.Undo();
            _logic?.Render();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Y && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _undo.Redo();
            _logic?.Render();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Delete)
        {
            DeleteSelected();
            e.Handled = true;
        }
    }
    public void SetStatus(string message, bool isError = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError ? Brushes.Red : Brushes.Black;
    }
    private void InitBorderResize()
    {
        const double grip = 12;

        PageBorder.PointerMoved += (s, e) =>
        {
            if (_resizing) return;
            var p = e.GetPosition(PageBorder);
            bool onRight = p.X >= PageBorder.Bounds.Width - grip;
            bool onBottom = p.Y >= PageBorder.Bounds.Height - grip;
            if (onRight)
                PageBorder.Cursor = new Cursor(StandardCursorType.RightSide);
            else if (onBottom)
                PageBorder.Cursor = new Cursor(StandardCursorType.BottomSide);
            else
                PageBorder.Cursor = new Cursor(StandardCursorType.Arrow);
        };

        PageBorder.PointerPressed += (s, e) =>
        {
            if (_logic == null) return;

            var p = e.GetPosition(PageBorder);

            _resizeRight = p.X >= PageBorder.Bounds.Width - grip;
            _resizeBottom = p.Y >= PageBorder.Bounds.Height - grip;

            if (_resizeRight || _resizeBottom)
            {
                _resizing = true;
                _resizeStart = p;

                _startW = _logic.PaperWidthMm;
                _startH = _logic.PaperHeightMm;

                e.Pointer.Capture(PageBorder);
                e.Handled = true;
            }
        };
        PageBorder.PointerMoved += (s, e) =>
        {
            if (!_resizing) return;
            if (_logic == null) return;

            var p = e.GetPosition(PageBorder);

            var dxPx = p.X - _resizeStart.X;
            var dyPx = p.Y - _resizeStart.Y;

            if (_resizeRight)
            {
                _logic.PaperWidthMm =
                    Math.Max(50, _startW + dxPx / 3.7795);
            }

            if (_resizeBottom)
            {
                _logic.PaperHeightMm =
                    Math.Max(50, _startH + dyPx / 3.7795);
            }

            _logic.ApplyPaperSize();
        };
        PageBorder.PointerReleased += (s, e) =>
        {
            if (_resizing)
            {
                _resizing = false;
                _resizeRight = false;
                _resizeBottom = false;
                e.Pointer.Capture(null);
                _logic?.Render();  // ★追加
            }
        };
    }
    private void Portrait_Click(object? sender, RoutedEventArgs e)
    {
        if (_logic == null) return;
        var w = Math.Min(_logic.PaperWidthMm, _logic.PaperHeightMm);
        var h = Math.Max(_logic.PaperWidthMm, _logic.PaperHeightMm);
        _logic.SetPaperSize(w, h);
        _logic.Orientation  = 1;
        _logic.IsLandscape  = false;
        _logic.ApplyPaperSize();
        _logic.Render();
        PaperWidthBox.Text  = _logic.PaperWidthMm.ToString("0.##");
        PaperHeightBox.Text = _logic.PaperHeightMm.ToString("0.##");
        UpdateOrientationButtons();
        Debug.WriteLine("PORTRAIT");
    }
    private void Landscape_Click(object? sender, RoutedEventArgs e)
    {
        if (_logic == null) return;
        var w = Math.Max(_logic.PaperWidthMm, _logic.PaperHeightMm);
        var h = Math.Min(_logic.PaperWidthMm, _logic.PaperHeightMm);
        _logic.SetPaperSize(w, h);
        _logic.Orientation  = 2;
        _logic.IsLandscape  = true;
        _logic.ApplyPaperSize();
        _logic.Render();
        PaperWidthBox.Text  = _logic.PaperWidthMm.ToString("0.##");
        PaperHeightBox.Text = _logic.PaperHeightMm.ToString("0.##");
        UpdateOrientationButtons();
        Debug.WriteLine("LANDSCAPE");
    }
    private void PageCanvas_BackgroundPressed(object? sender, PointerPressedEventArgs e)
    {
        // ★Canvas背景のみ
        if (!ReferenceEquals(e.Source, PageCanvas))
            return;
        _currentTool = ToolType.None;
        _logic?.ClearSelection();
    }
    private void GridCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        Debug.WriteLine("GRID EVENT FIRED");
        Debug.WriteLine("Render logic hash = " + this.GetHashCode());
        if (_logic == null) return;
        if (GridCombo.SelectedItem is not ComboBoxItem item) return;
        string text = item.Content?.ToString() ?? "";
        // "5mm" → "5"
        if (text.EndsWith("mm"))
            text = text.Replace("mm", "");
        if (!double.TryParse(text, out double mm))
            return;
        Debug.WriteLine($"GRID CHANGE → {mm}mm");
        _logic.GridMm = mm;
        _logic.Render();
    }
    private void ShowPageProperties()
    {
        if (_logic == null) return;

        _pageSelected = true;

        AddNumberRow("PaperWidth",
            _logic.PaperWidthMm,
            v =>
            {
                _logic.SetPaperSize(v, _logic.PaperHeightMm);
                _logic.ApplyPaperSize();
            });

        AddNumberRow("PaperHeight",
            _logic.PaperHeightMm,
            v =>
            {
                _logic.SetPaperSize(_logic.PaperWidthMm, v);
                _logic.ApplyPaperSize();
            });

        AddBoolRow("Landscape",
            _logic.IsLandscape,
            v =>
            {
                if (v)
                    _logic.SetPaperSize(
                        _logic.PaperHeightMm,
                        _logic.PaperWidthMm);
                else
                    _logic.SetPaperSize(
                        Math.Min(_logic.PaperWidthMm, _logic.PaperHeightMm),
                        Math.Max(_logic.PaperWidthMm, _logic.PaperHeightMm));

                _logic.ApplyPaperSize();
            });
    }
    private void OnControlSelected(DesignControl ctrl)
    {
        if (_selectionChanging) return;

        _selectionChanging = true;
        try
        {
            _selection?.SelectSingle(ctrl);

            Dispatcher.UIThread.Post(() =>
            {
                SyncTreeSelection(ctrl);
                ShowProperties(ctrl);
                TopLevel.GetTopLevel(this)?
                    .FocusManager?
                    .ClearFocus();

                PageCanvas.Focus();
            });
        }
        finally
        {
            _selectionChanging = false;
        }
    }
    private static void AddStyleRows(DesignControl ctrl, List<PropertyRow> rows)
    {
        //if (string.IsNullOrWhiteSpace(ctrl.Style))
        //    return;
        //var styleMap = ParseStyle(ctrl.Style);
        //if (styleMap.TryGetValue("color", out var c))
        //    rows.Add(new PropertyRow("Color", c, true));
        //if (styleMap.TryGetValue("background-color", out var bg))
        //    rows.Add(new PropertyRow("BackColor", bg, true));
        //if (styleMap.TryGetValue("font-family", out var ff))
        //    rows.Add(new PropertyRow("FontFamily", ff, true));

        //if (styleMap.TryGetValue("font-size", out var fs))
        //    rows.Add(new PropertyRow("FontSize", fs, true));

        //if (styleMap.TryGetValue("font-weight", out var fw))
        //    rows.Add(new PropertyRow("FontWeight", fw, true));

        //if (styleMap.TryGetValue("font-style", out var fst))
        //    rows.Add(new PropertyRow("FontStyle", fst, true));

        //if (styleMap.TryGetValue("text-align", out var ta))
        //    rows.Add(new PropertyRow("StyleTextAlign", ta, true));
    }

    private static Dictionary<string, string> ParseStyle(string style)
    {
        var dict = new Dictionary<string, string>();

        foreach (var raw in style.Split(';'))
        {
            var s = raw.Trim();
            if (s.Length == 0) continue;

            var idx = s.IndexOf(':');
            if (idx < 0) continue;

            var key = s.Substring(0, idx).Trim().ToLowerInvariant();
            var val = s.Substring(idx + 1).Trim();

            dict[key] = val;
        }

        return dict;
    }
    // =========================================================
    // ✅ Sectionクリック → PropertyGridGrid表示
    // =========================================================
    private void Tree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_fromTree) return;
        if (Tree.SelectedItem is not OutlineNode node) return;

        // =========================
        // ⭐ Page（これが正解）
        // =========================
        if (node.Target == null)
        {
            ShowPageProperties();
            return;
        }

        switch (node.Target)
        {
            case SectionDefinition sec:
                ShowSectionProperties(sec);
                break;

            case DesignControl ctrl:
                ShowProperties(ctrl);
                break;
        }
    }
    private void ShowSectionProperties(SectionDefinition sec)
    {
        // 既存のプロパティ行をクリア
        PropertyRows.Clear();

        // セクションのプロパティを追加
        PropertyRows.Add(new PropertyRow("Section Name", sec.Name, false));
        PropertyRows.Add(new PropertyRow("Kind", sec.Kind.ToString(), false));

        // Height：編集可能でロジックに即時反映
        var heightRow = new PropertyRow("Height (mm)", sec.HeightMm.ToString("0.###"), true, "numeric");
        heightRow.ApplyWithOldNew = (oldS, newS) =>
        {
            if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
            if (Math.Abs(o - n) < 0.001) return;
            _undo.Execute(new UndoRedo.PropertyChangeCommand(
                redo: () => { sec.HeightMm = n; _logic?.Render(); },
                undo: () => { sec.HeightMm = o; _logic?.Render(); }
            ));
        };
        PropertyRows.Add(heightRow);

        PropertyRows.Add(new PropertyRow("Controls", sec.Controls.Count.ToString(), false));

        // rowsPerPage（ACR.Detail専用）
        if (sec.Name == "Detail")
        {
            var rppRow = new PropertyRow("rowsPerPage", sec.RowsPerPage.ToString(), true, "numeric");
            rppRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!int.TryParse(oldS, out var o) || !int.TryParse(newS, out var n)) return;
                if (o == n) return;
                _undo.Execute(new UndoRedo.PropertyChangeCommand(
                    redo: () => { sec.RowsPerPage = n; },
                    undo: () => { sec.RowsPerPage = o; }
                ));
            };
            PropertyRows.Add(rppRow);
        }

        // グループ関連（読み取り専用）
        PropertyRows.Add(new PropertyRow("Group Level", sec.GroupLevel.ToString(), false));
        PropertyRows.Add(new PropertyRow("Group Key Field", sec.GroupKeyField ?? "", false));
        PropertyRows.Add(new PropertyRow("Group New Page", sec.GroupNewPage.ToString(), false, "checkbox"));
        PropertyRows.Add(new PropertyRow("Repeat On New Page", sec.RepeatOnNewPage.ToString(), false, "checkbox"));
        PropertyRows.Add(new PropertyRow("Keep Together", sec.KeepTogether.ToString(), false, "checkbox"));

    }
    private static string UpdateStyle(string style, string key, string value)
    {
        var dict = string.IsNullOrWhiteSpace(style) ? new Dictionary<string, string>() : ParseStyle(style);
        dict[key] = value;
        return string.Join("; ", dict.Select(kv => $"{kv.Key}: {kv.Value}"));
    }
    //
    //グリッド
    private void ApplyGridToLogic()
    {
        if (_logic == null) return;
        _logic.GridMm = GridCombo.SelectedIndex switch
        {
            0 => 1,
            1 => 5,
            2 => 10,
            _ => 1
        };
    }
    // =========================================================
    // ✅ 新規・クリア・ロード・保存
    // =========================================================
    private void New_Click(object? sender, RoutedEventArgs e)
    {
        NewButton.IsEnabled = false;
        LoadButton.IsEnabled = false;
        EnableEditing();
        ApplyNewDefaultUI();
    }
    private void ApplyNewDefaultUI()
    {
        if (_logic == null) return;
        // A4を文字で探す（順番に依存しない）
        foreach (ComboBoxItem item in PaperSizeCombo.Items)
        {
            if (item.Content?.ToString() == "A4")
            {
                PaperSizeCombo.SelectedItem = item;
                break;
            }
        }
        // 横向き
        _isLandscape = true;
        UpdateOrientationButtons();
        // サイズ表示
        PaperWidthBox.Text = _logic.PaperWidthMm.ToString("0.##");
        PaperHeightBox.Text = _logic.PaperHeightMm.ToString("0.##");
        // マージン
        MarginTopBox.Value = 1.00m;
        MarginBottomBox.Value = 1.00m;
        MarginLeftBox.Value = 1.00m;
        MarginRightBox.Value = 1.00m;
        GridCombo.SelectedIndex = 2; // 10mm
    }
    private void UpdateOrientationButtons()
    {
        if (_isLandscape)
        {
            LandscapeButton.Background = Brushes.LightSteelBlue;
            PortraitButton.Background = Brushes.Transparent;
        }
        else
        {
            PortraitButton.Background = Brushes.LightSteelBlue;
            LandscapeButton.Background = Brushes.Transparent;
        }
    }
    //private void PaperSizeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    //{
    //    if (_logic == null) return;
    //    if (PaperSizeCombo.SelectedItem is not ComboBoxItem item) return;

    //    string name = item.Content?.ToString() ?? "";
    //    _logic.ApplyPaperByName(name);
    //}
    private void PaperSizeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        if (_logic == null) return;
        _logic.ApplyPaperSize();
    }
    private void Clear_Click(object? sender, RoutedEventArgs e)
    {
        ClearCanvas();
    }
    private void ClearCanvas()
    {
        _logic?.Clear();
        MarginTopBox.Value = 1.00m;
        MarginBottomBox.Value = 1.00m;
        MarginLeftBox.Value = 1.00m;
        MarginRightBox.Value = 1.00m;
        if (_logic != null)
        {
            _logic.SetMarginsMm(1.0, 1.0, 1.0, 1.0);
            _logic.GridMm = 1;
        }
        GridCombo.SelectedIndex = 0;
        Tree.ItemsSource = null;
        _selection?.Clear();
        _currentSection = null;
        _pageSelected = false;
        _pageReady = false;
        UpdateUIEnabled();
    }
    private async void Load_Click(object? sender, RoutedEventArgs e)
    {
        LoadButton.IsEnabled = false;
        var top = TopLevel.GetTopLevel(this);
        if (top == null)
        {
            LoadButton.IsEnabled = true;
            return;
        }
        string templatePath =
            System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "Template");
        IStorageFolder? startFolder = null;
        if (Directory.Exists(templatePath))
        {
            startFolder = await top.StorageProvider
                .TryGetFolderFromPathAsync(templatePath);
        }
        var files = await top.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "テンプレートを選択",
                AllowMultiple = false,
                SuggestedStartLocation = startFolder,
                FileTypeFilter = new[]
                {
                new FilePickerFileType("Across Template")
                {
                    Patterns = new[] { "*.acr", "*.arc", "*.json" }
                }
                }
            });
        if (files.Count == 0)
        {
            LoadButton.IsEnabled = true;
            return;
        }
        string path = files[0].Path.LocalPath;
        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (ext != ".acr" && ext != ".arc" && ext != ".json")
        {
            ShowStatus("ACR形式のみ読み込み可能です。", true);
            LoadButton.IsEnabled = true;
            return;
        }
        string? json;
        if (ext == ".json")
        {
            json = System.IO.File.ReadAllText(path);  // ★ .jsonはそのまま読む
        }
        else
        {
            json = LoadTemplateFromArc(path);
        }
        if (_logic == null)
        {
            LoadButton.IsEnabled = true;
            return;
        }
        // -----------------------------
        // ★ ロード中フラグON
        // -----------------------------
        _isLoading = true;
        _logic.SetJsonFromString(json);
        _logic.Render();
        SyncPageSettingsFromLogic();
        SetPaperComboBySize(
            _logic.PaperWidthMm,
            _logic.PaperHeightMm);
        _isLoading = false;
        _pageReady = true;
        UpdateUIEnabled();
        LoadButton.IsEnabled = false;
    }
    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (_logic == null) return;
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        string defaultName =
            "AcrReport_" +
            DateTime.Now.ToString("yyyyMMddHHmmss") +
            ".acr";
        string templatePath =
            System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "Template");
        var dialog = new SaveFileDialog
        {
            Title = "保存",
            InitialFileName = defaultName,
            Directory = templatePath,
            Filters =
            {
                new FileDialogFilter
                {
                    Name = "Across Report",
                    Extensions = { "acr" }
                }
            }
        };
        var path = await dialog.ShowAsync(window);
        if (string.IsNullOrWhiteSpace(path)) return;
        // 🔥 ここが重要
        if (!path.EndsWith(".acr", StringComparison.OrdinalIgnoreCase))
        {
            path += ".acr";
        }
        _logic.SaveAcr(path);
    }
    // =========================================================
    // ✅ PropertyGrid 行データ（編集通知付き）
    // =========================================================
    private void UpdateUIEnabled()
    {
        if (!_pageReady)
        {
            NewButton.IsEnabled = true;
            LoadButton.IsEnabled = true;
            SaveButton.IsEnabled = false;
            ClearButton.IsEnabled = false;
            Tree.IsEnabled = false;
            PageCanvas.IsEnabled = false;
            PageBorder.IsVisible = false;
            return;
        }
        NewButton.IsEnabled = false;
        LoadButton.IsEnabled = false;
        SaveButton.IsEnabled = true;
        ClearButton.IsEnabled = true;
        Tree.IsEnabled = true;
        PageCanvas.IsEnabled = true;
        PageBorder.IsVisible = true;
    }
    private void PaperSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (PaperSizeCombo.SelectedItem == null) return;
        EnableEditing();
    }
    private void EnableEditing()
    {
        if (_pageReady) return;
        _pageReady = true;
        _logic?.NewReport();
        UpdateUIEnabled();
    }
    private OutlineNode? _lastTreeSelectedNode;
   
    //private void OnGridChanged(double gridMm)
    //{
    //    if (_logic == null) return;
    //    _logic.GridMm = gridMm;
    //}
    private void PageCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_logic == null) return;
        var pt = e.GetCurrentPoint(PageCanvas);
        // =========================
        // 右クリック
        // =========================
        if (pt.Properties.IsRightButtonPressed)
        {
            e.Handled = true;
            return;
        }
        var pPage = e.GetPosition(PageCanvas);
        // =========================
        // Tool処理
        // =========================
        if (_currentTool == ToolType.Label)
        {
            _logic.CreateControlFromTool("Label", pPage.X, pPage.Y);
            _logic.Render();
            var c = _logic.GetLastAddedControl();
            if (c != null) { _logic.SelectControl(c); ShowProperties(c); }
            _currentTool = ToolType.None;
            e.Handled = true;
            return;
        }
        if (_currentTool == ToolType.TextBox)
        {
            _logic.CreateControlFromTool("TextBox", pPage.X, pPage.Y);
            _logic.Render();
            var c = _logic.GetLastAddedControl();
            if (c != null) { _logic.SelectControl(c); ShowProperties(c); }
            _currentTool = ToolType.None;
            e.Handled = true;
            return;
        }
        if (_currentTool == ToolType.Shape)
        {
            _logic.CreateControlFromTool("Shape", pPage.X, pPage.Y);
            _logic.Render();
            var c = _logic.GetLastAddedControl();
            if (c != null) { _logic.SelectControl(c); ShowProperties(c); }
            _currentTool = ToolType.None;
            e.Handled = true;
            return;
        }
        if (_currentTool == ToolType.Line)
        {
            _drawingLine = true;
            _logic.IsDrawingLine = true;
            _lineStartPx = pPage;
            _previewLine = new Line
            {
                StartPoint = pPage,
                EndPoint = pPage,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            PageCanvas.Children.Add(_previewLine);
            e.Pointer.Capture(PageCanvas);
            e.Handled = true;
            return;
        }
        // =========================
        // 範囲選択（背景のみ）
        // =========================
        if (_currentTool == ToolType.None && e.Source == PageCanvas)
        {
            _selecting = true;
            _selectStart = pPage;
            _selectionRect = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 1,
                StrokeDashArray = new AvaloniaList<double> { 4, 4 },
                Fill = new SolidColorBrush(Color.FromArgb(40, 0, 120, 255)),
                IsHitTestVisible = false
            };
            PageCanvas.Children.Add(_selectionRect);
            e.Pointer.Capture(PageCanvas);
            e.Handled = true;
        }
    }
    private void PageCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_drawingLine && _previewLine != null)
        {
            var p = e.GetPosition(PageCanvas);
            _previewLine.EndPoint = p;
            e.Handled = true;
            return;
        }
        if (!_rightDown) return;
        var pos = e.GetPosition(this);
        var delta = pos - _panStart;
        if (!_panning && Math.Abs(delta.X) + Math.Abs(delta.Y) > 4) _panning = true;
        if (_panning)
        {
            MainScroll.Offset = new Vector(
                _scrollStart.X - delta.X,
                _scrollStart.Y - delta.Y);
        }
        // =========================    
        // 範囲選択中        
        // =========================
        if (_selecting && _selectionRect != null)
        {
            var p = e.GetPosition(PageCanvas);

            var x = Math.Min(p.X, _selectStart.X);
            var y = Math.Min(p.Y, _selectStart.Y);
            var w = Math.Abs(p.X - _selectStart.X);
            var h = Math.Abs(p.Y - _selectStart.Y);
            Canvas.SetLeft(_selectionRect, x);
            Canvas.SetTop(_selectionRect, y);
            _selectionRect.Width = w;
            _selectionRect.Height = h;
            e.Handled = true;
            return;
        }
    }
    private void PageCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // =========================
        // ✅ 範囲選択確定（最優先）
        // =========================
        if (_selecting && _selectionRect != null)
        {
            var rect = new Rect(
                Canvas.GetLeft(_selectionRect),
                Canvas.GetTop(_selectionRect),
                _selectionRect.Width,
                _selectionRect.Height);
            var add = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            //セレクションマネージャーに選択処理を任せる（これが無いと選択できない）
            _selection?.SelectByRect(rect, add);
            PageCanvas.Children.Remove(_selectionRect);
            _selectionRect = null;
            _selecting = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }
        // =========================
        // ✅ Line描画確定
        // =========================
        if (!_drawingLine || _previewLine == null) return;
        e.Pointer.Capture(null);
        var endPx = e.GetPosition(PageCanvas);
        PageCanvas.Children.Remove(_previewLine);
        _drawingLine = false;
        _logic!.IsDrawingLine = false;
        _previewLine = null;

        // HeaderPxを考慮したセクション解決
        var (secStart, x1, y1) = _logic.ResolveCanvasPxToSection(_lineStartPx.X, _lineStartPx.Y);
        var (secEnd,   x2, y2) = _logic.ResolveCanvasPxToSection(endPx.X, endPx.Y);

        // 両端が同じセクション内でないと無効
        var sec = secStart ?? secEnd;
        if (sec == null) return;
        if (secStart != null && secEnd != null && secStart != secEnd)
        {
            // 異なるセクションにまたがる場合は開始セクションを優先
            sec = secStart;
        }

        if (Math.Abs(x2 - x1) < 0.2 && Math.Abs(y2 - y1) < 0.2) return;

        var ctrl = new DesignControl
        {
            Type = "Line",
            Name = $"Line{_logic.NextLineId()}",
            X1Mm = x1,
            Y1Mm = y1,
            X2Mm = x2,
            Y2Mm = y2,
            LeftMm   = Math.Min(x1, x2),
            TopMm    = Math.Min(y1, y2),
            WidthMm  = Math.Max(1, Math.Abs(x2 - x1)),
            HeightMm = Math.Max(1, Math.Abs(y2 - y1))
        };
        sec.Controls.Add(ctrl);
        _logic.Render();
        ShowProperties(ctrl);
        e.Handled = true;
        _currentTool = ToolType.None;
    }
    private void MoveToFront()
    {
        var ctrl = _selection?.Primary;
        if (ctrl == null) return;
        _logic?.MoveControlToFront(ctrl);
    }
    private void MoveToBack()
    {
        var ctrl = _selection?.Primary;
        if (ctrl == null) return;
        _logic?.MoveControlToBack(ctrl);
    }
    private void AddGroup()
    {
        _logic?.AddGroupSection();
    }
    // =========================================================
    // ✅ ツールボタン登録（Click + キャプチャ開始のみ）
    // =========================================================
    private void WireToolButton(Button btn, EventHandler<RoutedEventArgs> clickHandler)
    {
        btn.Click         += clickHandler;
        btn.PointerPressed += ToolButton_PointerPressed;
    }
    // =========================================================
    // ✅ ポインターキャプチャ方式ドラッグ
    //    ToolButton.PointerPressed  → キャプチャ開始、ゴースト表示
    //    UserControl.PointerMoved   → ゴースト追従
    //    UserControl.PointerReleased→ PageCanvas上ならコントロール生成
    // =========================================================
    private string?  _toolDragKind;       // ドラッグ中のツール種別
    private Border?  _toolGhost;          // ゴースト（半透明プレビュー）
    private bool     _toolDragging;       // ドラッグ中フラグ
    private IPointer? _toolPointer;       // キャプチャ中のポインター

    private void ToolButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_pageReady) return;
        if (sender is not Button btn) return;
        var kind = btn.Tag?.ToString();
        if (string.IsNullOrEmpty(kind)) return;

        _toolDragKind  = kind;
        _toolDragging  = false;
        _toolPointer   = e.Pointer;

        // UserControl全体でPointerを捕捉 → ボタン外に出てもMovedが来る
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void UserControl_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_toolDragKind == null) return;
        if (!ReferenceEquals(e.Pointer, _toolPointer)) return;

        var pos = e.GetPosition(this);   // UserControl基準

        if (!_toolDragging)
        {
            // ゴースト生成（初回移動時）
            _toolDragging = true;
            _toolGhost = new Border
            {
                Width            = 60,
                Height           = 22,
                Background       = new SolidColorBrush(Color.FromArgb(180, 100, 160, 255)),
                BorderBrush      = Brushes.DodgerBlue,
                BorderThickness  = new Thickness(1),
                CornerRadius     = new CornerRadius(3),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text                = _toolDragKind,
                    Foreground          = Brushes.White,
                    FontSize            = 11,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center
                }
            };
            GhostLayer.Children.Add(_toolGhost);
        }

        // ゴースト位置をマウスに追従（中心をカーソルに合わせる）
        if (_toolGhost != null)
        {
            Canvas.SetLeft(_toolGhost, pos.X - _toolGhost.Width  / 2);
            Canvas.SetTop (_toolGhost, pos.Y - _toolGhost.Height / 2);
        }
    }

    private void UserControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_toolDragKind == null) return;
        if (!ReferenceEquals(e.Pointer, _toolPointer)) return;

        // キャプチャ解放
        e.Pointer.Capture(null);

        // ゴースト除去
        RemoveToolGhost();

        if (!_toolDragging)
        {
            // ほぼ動かさずに離した → クリック扱い（_currentToolは既にClick側でセット済み）
            _toolDragKind = null;
            _toolPointer  = null;
            return;
        }

        var kind = _toolDragKind;
        _toolDragKind = null;
        _toolPointer  = null;
        _toolDragging = false;

        if (_logic == null) return;

        // ====================================
        // PageCanvas 上にリリースされたか判定
        // ====================================
        var posOnCanvas = e.GetPosition(PageCanvas);
        var canvasBounds = new Rect(0, 0, PageCanvas.Bounds.Width, PageCanvas.Bounds.Height);

        if (!canvasBounds.Contains(posOnCanvas))
        {
            Debug.WriteLine("[CAPTURE] リリース位置がCanvas外");
            return;
        }

        Debug.WriteLine($"[CAPTURE DROP] kind={kind} pos=({posOnCanvas.X:F1},{posOnCanvas.Y:F1})");

        // ====================================
        // コントロール生成
        // ====================================
        _logic.CreateControlFromTool(kind, posOnCanvas.X, posOnCanvas.Y);
        _logic.Render();

        var newCtrl = _logic.GetLastAddedControl();
        if (newCtrl != null)
        {
            _logic.SelectControl(newCtrl);
            ShowProperties(newCtrl);
        }

        _currentTool = ToolType.None;
    }

    private void RemoveToolGhost()
    {
        if (_toolGhost == null) return;
        GhostLayer.Children.Remove(_toolGhost);
        _toolGhost = null;
    }
    private void ToolLabel_Click(object? sender, RoutedEventArgs e)
    {
        _currentTool = ToolType.Label;
        Debug.WriteLine("TOOL = Label");
    }
    private void ToolText_Click(object? sender, RoutedEventArgs e)
    {
        _currentTool = ToolType.TextBox;
        Debug.WriteLine("TOOL = TextBox");
    }
    private void ToolLine_Click(object? sender, RoutedEventArgs e)
    {
        _currentTool = ToolType.Line;
        Debug.WriteLine("TOOL = Line");
    }
    private void ToolShape_Click(object? sender, RoutedEventArgs e)
    {
        _currentTool = ToolType.Shape;
        Debug.WriteLine("TOOL = Shape");
    }
    private void BringFront(DesignControl? ctrl)
    {
        if (ctrl == null) return;
        ctrl.ZIndex += 1000;
    }
    private void SendBack(DesignControl? ctrl)
    {
        if (ctrl == null) return;
        ctrl.ZIndex -= 1000;
    }
    private void PageCanvas_ContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (_logic == null)return;
        Debug.WriteLine("RIGHT CLICK DETECTED");
        // クリック位置取得
        if (!e.TryGetPosition(PageCanvas, out var pos)) return;
        // クリック元をたどってControl特定
        Control? srcCtrl = e.Source as Control;
        DesignControl? hitCtrl = null;
        while (srcCtrl != null)
        {
            if (srcCtrl is DesignControlView dcv)
            {
                hitCtrl = dcv.Model;
                break;
            }
            if (srcCtrl is LineView lv)
            {
                hitCtrl = lv.Model;
                break;
            }

            srcCtrl = srcCtrl.Parent as Control;
        }
        // =========================
        // 選択状態更新
        // =========================
        if (hitCtrl != null)
            _selection?.SelectSingle(hitCtrl);
        else
            _logic.ClearSelection();

        // =========================
        // コンテキストメニュー表示
        // =========================
        _contextMenu?.Show(pos);
        e.Handled = true;
    }
    //private void ApplyPropertyToModel(DesignControl ctrl,string key,object? value)
    //{
    //    var prop = ctrl.GetType().GetProperty(key);
    //    if (prop == null) return;
    //    try
    //    {
    //        var t = prop.PropertyType;
    //        var converted = Convert.ChangeType(value, t);
    //        prop.SetValue(ctrl, converted);
    //    }
    //    catch { }
    //}
    private async void PropertyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not PropertyRow row) return;
        switch (row.Editor)
        {
            case "fontpicker":
                await OpenFontPicker(row);
                break;
            case "colorpicker":
                await OpenColorPicker(row);
                break;
        }
    }
    private async Task OpenFontPicker(PropertyRow row)
    {
        var list = new ListBox
        {
            ItemsSource = _fontNames
        };
        var win = new Window
        {
            Title = "Font",
            Width = 300,
            Height = 400,
            Content = list
        };
        list.DoubleTapped += (_, __) =>
        {
            row.Value = list.SelectedItem?.ToString() ?? "";
            win.Close();
        };
        await win.ShowDialog((Window)TopLevel.GetTopLevel(this)!);
    }
    private async Task OpenColorPicker(PropertyRow row)
    {
        var box = new TextBox { Text = row.Value };
        var ok = new Button { Content = "OK" };

        var panel = new StackPanel
        {
            Margin = new Thickness(10),
            Spacing = 8,
            Children = { box, ok }
        };
        var win = new Window
        {
            Title = "Color (#RRGGBB)",
            Width = 260,
            Height = 140,
            Content = panel
        };
        ok.Click += (_, __) =>
        {
            row.Value = box.Text ?? "";
            win.Close();
        };
        await win.ShowDialog((Window)TopLevel.GetTopLevel(this)!);
    }
    //private OutlineNode? FindNodeByTarget(IEnumerable<OutlineNode> roots, object target)
    //{
    //    foreach (var r in roots)
    //    {
    //        var hit = FindNodeByTargetRec(r, target);
    //        if (hit != null) return hit;
    //    }
    //    return null;
    //}
    private OutlineNode? FindNodeByTargetRec(OutlineNode node, object target)
    {
        if (ReferenceEquals(node.Target, target)) return node;
        foreach (var ch in node.Children)
        {
            var hit = FindNodeByTargetRec(ch, target);
            if (hit != null) return hit;
        }
        return null;
    }
    // ⑥ Treeを全展開してからターゲットノードを選択
    private void ExpandAllNodes(IEnumerable<OutlineNode> nodes)
    {
        foreach (var n in nodes)
        {
            n.IsExpanded = true;
            if (n.Children?.Count > 0)
                ExpandAllNodes(n.Children);
        }
    }
    private void SyncTreeSelection(DesignControl ctrl)
    {
        if (Tree.ItemsSource is not IEnumerable<OutlineNode> roots) return;
        ExpandAllNodes(roots);

        OutlineNode? hitNode = null;
        foreach (var r in roots)
        {
            hitNode = FindNodeByControl(r, ctrl);
            if (hitNode != null) break;
        }
        if (hitNode == null) return;

        if (ReferenceEquals(Tree.SelectedItem, hitNode)) return;
        _selectionChanging = true;
        try
        {
            Tree.SelectedItem = hitNode;
            // ★追加：選択ノードをスクロールして表示
            Tree.ScrollIntoView(hitNode);
        }
        finally
        {
            _selectionChanging = false;
        }
    }
    private OutlineNode? FindNode(OutlineNode n, object target)
    {
        if (ReferenceEquals(n.Target, target))return n;
        foreach (var c in n.Children)
        {
            var r = FindNode(c, target);
            if (r != null) return r;
        }
        return null;
    }
    private OutlineNode? FindNodeByTarget(OutlineNode node, object target)
    {
        if (ReferenceEquals(node.Target, target)) return node;
        foreach (var c in node.Children)
        {
            var r = FindNodeByTarget(c, target);
            if (r != null) return r;
        }
        return null;
    }
    private OutlineNode? FindNodeByControl(OutlineNode node, DesignControl ctrl)
    {
        if (node.Target == ctrl) return node;
        if (node.Children != null)
        {
            foreach (var c in node.Children)
            {
                var r = FindNodeByControl(c, ctrl);
                if (r != null) return r;
            }
        }
        return null;
    }
    private void OnLogicControlSelected(DesignControl ctrl)
    {
        SyncTreeSelection(ctrl);
    }
    private TreeViewItem? FindTreeItem(object target)
    {
        return Tree.GetRealizedContainers()
                   .OfType<TreeViewItem>()
                   .FirstOrDefault(x => x.DataContext == target);
    }
    private void ExpandToRoot(TreeViewItem item)
    {
        while (item != null)
        {
            item.IsExpanded = true;
            item = item.Parent as TreeViewItem;
        }
    }
    private OutlineNode? FindParentNode(OutlineNode target)
    {
        if (Tree.ItemsSource is not IEnumerable<OutlineNode> roots)
            return null;

        foreach (var r in roots)
        {
            var hit = FindParentNodeRecursive(r, target);
            if (hit != null)
                return hit;
        }
        return null;
    }
    private OutlineNode? FindParentNodeRecursive(
        OutlineNode root,
        OutlineNode target)
    {
        foreach (var c in root.Children)
        {
            if (c == target)
                return root;

            var hit = FindParentNodeRecursive(c, target);
            if (hit != null)
                return hit;
        }
        return null;
    }
    private void SyncPageSettingsFromLogic()
    {
        if (_logic == null) return;
        // 用紙サイズ
        PaperWidthBox.Text = _logic.PaperWidthMm.ToString("0");
        PaperHeightBox.Text = _logic.PaperHeightMm.ToString("0");
        // マージン
        double topMm = UnitConverter.TwipsToMm(_logic.TopMarginTwip);
        double bottomMm = UnitConverter.TwipsToMm(_logic.BottomMarginTwip);
        double leftMm = UnitConverter.TwipsToMm(_logic.LeftMarginTwip);
        double rightMm = UnitConverter.TwipsToMm(_logic.RightMarginTwip);
        MarginTopBox.Value = (decimal)topMm;
        MarginBottomBox.Value = (decimal)bottomMm;
        MarginLeftBox.Value = (decimal)leftMm;
        MarginRightBox.Value = (decimal)rightMm;
        // サイズ名
        SetPaperComboBySize(_logic.PaperWidthMm, _logic.PaperHeightMm);
        // 向き：ボタンのハイライトを統一
        _isLandscape = _logic.IsLandscape;
        UpdateOrientationButtons();
    }
    private void SetPaperComboBySize(double w, double h)
    {
        foreach (ComboBoxItem item in PaperSizeCombo.Items)
        {
            string name = item.Content?.ToString() ?? "";

            if (name == "A4" &&
                ((Near(w, 210) && Near(h, 297)) ||
                 (Near(w, 297) && Near(h, 210))))
            {
                PaperSizeCombo.SelectedItem = item;
                return;
            }

            if (name == "A3" &&
                ((Near(w, 297) && Near(h, 420)) ||
                 (Near(w, 420) && Near(h, 297))))
            {
                PaperSizeCombo.SelectedItem = item;
                return;
            }

            if (name == "B4" &&
                ((Near(w, 257) && Near(h, 364)) ||
                 (Near(w, 364) && Near(h, 257))))
            {
                PaperSizeCombo.SelectedItem = item;
                return;
            }

            if (name == "B5" &&
                ((Near(w, 182) && Near(h, 257)) ||
                 (Near(w, 257) && Near(h, 182))))
            {
                PaperSizeCombo.SelectedItem = item;
                return;
            }
        }
        // 一致しない場合はユーザー定義
        foreach (ComboBoxItem item in PaperSizeCombo.Items)
        {
            if (item.Content?.ToString() == "ユーザー定義")
            {
                PaperSizeCombo.SelectedItem = item;
                return;
            }
        }
    }
    private string? LoadTemplateFromArc(string path)
    {
        Debug.WriteLine("ARC opened: " + path);
        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(path);
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
            _logic?.StatusHandler?.Invoke("ACR-002: ARC内にJSONファイルが見つかりません。",true);
            return null;
        }
        catch (Exception ex)
        {
            _logic?.StatusHandler?.Invoke("ACR-003: ARC読込エラー: { ex.Message}", true);
            return null;
        }
    }
    public void ShowStatus(string message, bool isError = false)
    {
        StatusText.Text = message;

        if (isError)
            StatusText.Foreground = Brushes.Red;
        else
            StatusText.Foreground = Brushes.Black;
    }
    private void DeleteSelected()
    {
        if (_logic == null || _selection == null)
            return;
        foreach (var ctrl in _selection.MultiSelected.ToList())
        {
            _logic.DeleteControl(ctrl);
        }
        _selection.Clear();
    }
    private void ShowProperties(DesignControl ctrl)
    {
        PropertyRows.Clear();
        PropertyRows.Add(new PropertyRow("Type", ctrl.Type, false));

        // ③ Name を編集可能に
        var nameRow = new PropertyRow("Name", ctrl.Name, true);
        nameRow.ApplyWithOldNew = (oldS, newS) =>
        {
            if (oldS == newS) return;
            _undo.Execute(new PropertyChangeCommand(
                redo: () => { ctrl.Name = newS; _logic?.Render(); },
                undo: () => { ctrl.Name = oldS; _logic?.Render(); }
            ));
        };
        PropertyRows.Add(nameRow);

        // Left
        var leftRow = new PropertyRow("Left", ctrl.LeftMm.ToString("0.###"), true, "numeric");
        leftRow.ApplyWithOldNew = (oldS, newS) =>
        {
            if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
            if (Math.Abs(o - n) < 0.0001) return;
            _undo.Execute(new PropertyChangeCommand(
                redo: () => { ctrl.LeftMm = n; _logic?.UpdateControl(ctrl); },
                undo: () => { ctrl.LeftMm = o; _logic?.UpdateControl(ctrl); }
            ));
        };
        PropertyRows.Add(leftRow);

        // Top
        var topRow = new PropertyRow("Top", ctrl.TopMm.ToString("0.###"), true, "numeric");
        topRow.ApplyWithOldNew = (oldS, newS) =>
        {
            if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
            if (Math.Abs(o - n) < 0.0001) return;
            _undo.Execute(new PropertyChangeCommand(
                redo: () => { ctrl.TopMm = n; _logic?.UpdateControl(ctrl); },
                undo: () => { ctrl.TopMm = o; _logic?.UpdateControl(ctrl); }
            ));
        };
        PropertyRows.Add(topRow);

        // Width
        var widthRow = new PropertyRow("Width", ctrl.WidthMm.ToString("0.###"), true, "numeric");
        widthRow.ApplyWithOldNew = (oldS, newS) =>
        {
            if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
            if (Math.Abs(o - n) < 0.0001) return;
            _undo.Execute(new PropertyChangeCommand(
                redo: () => { ctrl.WidthMm = n; _logic?.UpdateControl(ctrl); },
                undo: () => { ctrl.WidthMm = o; _logic?.UpdateControl(ctrl); }
            ));
        };
        PropertyRows.Add(widthRow);
        // Height
        var heightRow = new PropertyRow("Height", ctrl.HeightMm.ToString("0.###"), true, "numeric");
        heightRow.ApplyWithOldNew = (oldS, newS) =>
        {
            if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
            if (Math.Abs(o - n) < 0.0001) return;
            _undo.Execute(new PropertyChangeCommand(
                redo: () => { ctrl.HeightMm = n; _logic?.UpdateControl(ctrl); },
                undo: () => { ctrl.HeightMm = o; _logic?.UpdateControl(ctrl); }
            ));
        };
        PropertyRows.Add(heightRow);
        // =========================
        // 共通表示系プロパティ
        // =========================
        if (!ctrl.Type.Contains("Line"))
        {
            // ForeColor
            var foreColorRow = new PropertyRow(
                "ForeColor",
                ctrl.ForeColor.ToString("X6").PadLeft(6, '0'),
                true);
            foreColorRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!int.TryParse(newS.TrimStart('#'),
                    System.Globalization.NumberStyles.HexNumber,
                    null, out var n)) return;
                if (!int.TryParse(oldS.TrimStart('#'),
                    System.Globalization.NumberStyles.HexNumber,
                    null, out var o)) return;
                if (o == n) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.ForeColor = n; _logic?.UpdateControl(ctrl); },
                    undo: () => { ctrl.ForeColor = o; _logic?.UpdateControl(ctrl); }
                ));
            };
            PropertyRows.Add(foreColorRow);

            // BackColor
            var backColorRow = new PropertyRow(
                "BackColor",
                ctrl.BackColor.ToString("X6").PadLeft(6, '0'),
                true);
            backColorRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!int.TryParse(newS.TrimStart('#'),
                    System.Globalization.NumberStyles.HexNumber,
                    null, out var n)) return;
                if (!int.TryParse(oldS.TrimStart('#'),
                    System.Globalization.NumberStyles.HexNumber,
                    null, out var o)) return;
                if (o == n) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.BackColor = n; _logic?.UpdateControl(ctrl); },
                    undo: () => { ctrl.BackColor = o; _logic?.UpdateControl(ctrl); }
                ));
            };
            PropertyRows.Add(backColorRow);

            // Alignment
            var alignRow = new PropertyRow(
                "Alignment",
                ctrl.Alignment,
                true, "combo",
                new[] { "Left", "Center", "Right" });
            alignRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (oldS == newS) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.Alignment = newS; ctrl.TextAlign = newS.ToLower(); _logic?.UpdateControl(ctrl); },
                    undo: () => { ctrl.Alignment = oldS; ctrl.TextAlign = oldS.ToLower(); _logic?.UpdateControl(ctrl); }
                ));
            };
            PropertyRows.Add(alignRow);

            // CanGrow
            var canGrowRow = new PropertyRow("CanGrow", ctrl.CanGrow.ToString(), true, "checkbox");
            canGrowRow.ApplyWithOldNew = (oldS, newS) =>
            {
                bool oldV = oldS == "True"; bool newV = newS == "True";
                if (oldV == newV) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.CanGrow = newV; },
                    undo: () => { ctrl.CanGrow = oldV; }
                ));
            };
            PropertyRows.Add(canGrowRow);

            // CanShrink
            var canShrinkRow = new PropertyRow("CanShrink", ctrl.CanShrink.ToString(), true, "checkbox");
            canShrinkRow.ApplyWithOldNew = (oldS, newS) =>
            {
                bool oldV = oldS == "True"; bool newV = newS == "True";
                if (oldV == newV) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.CanShrink = newV; },
                    undo: () => { ctrl.CanShrink = oldV; }
                ));
            };
            PropertyRows.Add(canShrinkRow);

            // MultiLine
            var multiLineRow = new PropertyRow("MultiLine", ctrl.MultiLine.ToString(), true, "checkbox");
            multiLineRow.ApplyWithOldNew = (oldS, newS) =>
            {
                bool oldV = oldS == "True"; bool newV = newS == "True";
                if (oldV == newV) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.MultiLine = newV; },
                    undo: () => { ctrl.MultiLine = oldV; }
                ));
            };
            PropertyRows.Add(multiLineRow);

            // Visible
            var visibleRow = new PropertyRow("Visible", ctrl.Visible.ToString(), true, "checkbox");
            visibleRow.ApplyWithOldNew = (oldS, newS) =>
            {
                bool oldV = oldS == "True"; bool newV = newS == "True";
                if (oldV == newV) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.Visible = newV; _logic?.UpdateControl(ctrl); },
                    undo: () => { ctrl.Visible = oldV; _logic?.UpdateControl(ctrl); }
                ));
            };
            PropertyRows.Add(visibleRow);
        }
        // =========================
        // 罫線系（Label/TextBox/Shape共通）
        // =========================
        if (ctrl.Type.Contains("Label") || ctrl.Type.Contains("TextBox") ||
            ctrl.Type.Contains("Shape") || ctrl.Type.Contains("Field"))
        {
            // LineColor（16進数表示）
            var lineColorRow = new PropertyRow(
                "LineColor",
                ctrl.LineColor.ToString("X6").PadLeft(6, '0'),
                true);
            lineColorRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!int.TryParse(newS.TrimStart('#'),
                    System.Globalization.NumberStyles.HexNumber,
                    null, out var n)) return;
                if (!int.TryParse(oldS.TrimStart('#'),
                    System.Globalization.NumberStyles.HexNumber,
                    null, out var o)) return;
                if (o == n) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.LineColor = n; _logic?.UpdateControl(ctrl); },
                    undo: () => { ctrl.LineColor = o; _logic?.UpdateControl(ctrl); }
                ));
            };
            PropertyRows.Add(lineColorRow);

            // LineWidth
            var lineWidthRow = new PropertyRow("LineWidth", ctrl.LineWidth.ToString("0.###"), true, "numeric");
            lineWidthRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
                if (Math.Abs(o - n) < 0.0001) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.LineWidth = n; _logic?.UpdateControl(ctrl); },
                    undo: () => { ctrl.LineWidth = o; _logic?.UpdateControl(ctrl); }
                ));
            };
            PropertyRows.Add(lineWidthRow);

            // LineStyle
            var lineStyleRow = new PropertyRow("LineStyle", ctrl.LineStyle.ToString(), true, "numeric");
            lineStyleRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!int.TryParse(oldS, out var o) || !int.TryParse(newS, out var n)) return;
                if (o == n) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.LineStyle = n; _logic?.UpdateControl(ctrl); },
                    undo: () => { ctrl.LineStyle = o; _logic?.UpdateControl(ctrl); }
                ));
            };
            PropertyRows.Add(lineStyleRow);
        }
        // =========================
        // Text系
        // =========================
        if (ctrl.Type.Contains("Label") || ctrl.Type.Contains("TextBox") ||
            ctrl.Type.Contains("Field"))
        {
            var textRow = new PropertyRow("Text", ctrl.Text, true);
            textRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (oldS == newS) return;

                _undo.Execute(
                    new PropertyChangeCommand(
                        redo: () =>
                        {
                            ctrl.Text = newS;
                            _logic?.UpdateControl(ctrl);
                        },
                        undo: () =>
                        {
                            ctrl.Text = oldS;
                            _logic?.UpdateControl(ctrl);
                        }
                    ));
            };
            PropertyRows.Add(textRow);

            // ④ FontFamily コンボ（OSフォント一覧）
            var osFonts = Avalonia.Media.FontManager.Current
                              .SystemFonts
                              .Select(f => f.Name)
                              .OrderBy(n => n)
                              .ToList();
            var fontFamilyRow = new PropertyRow(
                "FontFamily",
                string.IsNullOrEmpty(ctrl.FontFamily) ? "" : ctrl.FontFamily,
                true, "combo",
                osFonts);
            fontFamilyRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (oldS == newS) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.FontFamily = newS; ctrl.FontName = newS; _logic?.UpdateControl(ctrl); RequestRender(); },
                    undo: () => { ctrl.FontFamily = oldS; ctrl.FontName = oldS; _logic?.UpdateControl(ctrl); RequestRender(); }
                ));
            };
            PropertyRows.Add(fontFamilyRow);
            // FontSize
            var fontSizeRow = new PropertyRow("FontSize", ctrl.FontSizePt.ToString("0.#"), true, "numeric");
            fontSizeRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!double.TryParse(oldS, out var oldV)) return;
                if (!double.TryParse(newS, out var newV)) return;
                if (Math.Abs(oldV - newV) < 0.01) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.FontSizePt = newV; ctrl.FontSize = newV; _logic?.UpdateControl(ctrl); RequestRender(); },
                    undo: () => { ctrl.FontSizePt = oldV; ctrl.FontSize = oldV; _logic?.UpdateControl(ctrl); RequestRender(); }
                ));
            };
            PropertyRows.Add(fontSizeRow);

            var boldRow = new PropertyRow("Bold", ctrl.Bold.ToString(), true, "checkbox");
            boldRow.ApplyWithOldNew = (oldS, newS) =>
            {
                bool oldV = oldS == "true";
                bool newV = newS == "true";
                if (oldV == newV) return;
                _undo.Execute(
                    new PropertyChangeCommand(
                        redo: () =>
                        {
                            ctrl.Bold = newV;
                            RequestRender();
                        },
                        undo: () =>
                        {
                            ctrl.Bold = oldV;
                            RequestRender();
                        }
                    ));
            };
            PropertyRows.Add(boldRow);
        }
        // =========================
        // Line系
        // =========================
        if (ctrl.Type == "Line")
        {
            var x1Row = new PropertyRow("X1 (mm)", ctrl.X1Mm.ToString("0.###"), true, "numeric");
            x1Row.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.X1Mm = n; RequestRender(); },
                    undo: () => { ctrl.X1Mm = o; RequestRender(); }));
            };
            PropertyRows.Add(x1Row);

            var y1Row = new PropertyRow("Y1 (mm)", ctrl.Y1Mm.ToString("0.###"), true, "numeric");
            y1Row.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.Y1Mm = n; RequestRender(); },
                    undo: () => { ctrl.Y1Mm = o; RequestRender(); }));
            };
            PropertyRows.Add(y1Row);

            var x2Row = new PropertyRow("X2 (mm)", ctrl.X2Mm.ToString("0.###"), true, "numeric");
            x2Row.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.X2Mm = n; RequestRender(); },
                    undo: () => { ctrl.X2Mm = o; RequestRender(); }));
            };
            PropertyRows.Add(x2Row);

            var y2Row = new PropertyRow("Y2 (mm)", ctrl.Y2Mm.ToString("0.###"), true, "numeric");
            y2Row.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.Y2Mm = n; RequestRender(); },
                    undo: () => { ctrl.Y2Mm = o; RequestRender(); }));
            };
            PropertyRows.Add(y2Row);

            var lwRow = new PropertyRow("LineWidth", ctrl.LineWidth.ToString("0.###"), true, "numeric");
            lwRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.LineWidth = n; RequestRender(); },
                    undo: () => { ctrl.LineWidth = o; RequestRender(); }));
            };
            PropertyRows.Add(lwRow);
        }
        // =========================
        // Shape系
        // =========================
        if (ctrl.Type == "Shape")
        {
            var backStyleRow = new PropertyRow("BackStyle", ctrl.BackStyle.ToString(), true, "numeric");
            backStyleRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!int.TryParse(oldS, out var o) || !int.TryParse(newS, out var n)) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.BackStyle = n; RequestRender(); },
                    undo: () => { ctrl.BackStyle = o; RequestRender(); }));
            };
            PropertyRows.Add(backStyleRow);

            var radiusRow = new PropertyRow("Radius (mm)", ctrl.RoundingRadius.ToString("0.###"), true, "numeric");
            radiusRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (!double.TryParse(oldS, out var o) || !double.TryParse(newS, out var n)) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.RoundingRadius = n; RequestRender(); },
                    undo: () => { ctrl.RoundingRadius = o; RequestRender(); }));
            };
            PropertyRows.Add(radiusRow);
        }
        // DataField（Line以外）
        if (ctrl.Type != "Line")
        {
            var dataFieldRow = new PropertyRow("DataField", ctrl.DataField ?? "", true);
            dataFieldRow.ApplyWithOldNew = (oldS, newS) =>
            {
                if (oldS == newS) return;
                _undo.Execute(new PropertyChangeCommand(
                    redo: () => { ctrl.DataField = newS; _logic?.UpdateControl(ctrl); },
                    undo: () => { ctrl.DataField = oldS; _logic?.UpdateControl(ctrl); }
                ));
            };
            PropertyRows.Add(dataFieldRow);
        }
    }
    private void AddLabel(int row, string text)
    {
        var lbl = new TextBlock { Text = text };

        Grid.SetRow(lbl, row);
        Grid.SetColumn(lbl, 0);
    }
    private void AddTextRow(string name, string value, Action<string> apply)
    {

        var box = new TextBox { Text = value };

        box.LostFocus += (_, __) =>
            apply(box.Text ?? "");  

        Grid.SetColumn(box, 1);

    }
    private void AddNumberRow(string name, double value, Action<double> apply)
    {

        var box = new TextBox
        {
            Text = value.ToString()
        };

        box.LostFocus += (_, __) =>
        {
            if (double.TryParse(box.Text, out var v))
                apply(v);
            else
                box.Text = value.ToString();
        };
        Grid.SetColumn(box, 1);
    }
    private void AddBoolRow(string name, bool value, Action<bool> apply)
    {
        var chk = new CheckBox { IsChecked = value };
        chk.IsCheckedChanged += (_, __) =>
            apply(chk.IsChecked == true);
        Grid.SetColumn(chk, 1);
    }
    private void BindNumericUndo(
        PropertyRow row,
        DesignControl ctrl,
        Func<double> getter,
        Action<double> setter,
        string label)
    {
        row.ApplyWithOldNew = (oldS, newS) =>
        {
            if (!double.TryParse(oldS, out var oldV)) return;
            if (!double.TryParse(newS, out var newV)) return;
            if (Math.Abs(oldV - newV) < 0.0001) return;
            _undo.Execute(
                new PropertyChangeCommand(
                    redo: () =>
                    {
                        ctrl.Text = newS;
                        RequestRender();
                    },
                    undo: () =>
                    {
                        ctrl.Text = oldS;
                        RequestRender();
                    }
                ));
        };
    }
    private bool _renderQueued;
    private void RequestRender()
    {
        if (_renderQueued) return;
        _renderQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _renderQueued = false;
            _logic?.Render();
        }, DispatcherPriority.Background);
    }
    private void AddNumericUndoRow(
        string label,
        double currentValue,
        Action<double> setter)
    {
        var row = new PropertyRow(
            label,
            currentValue.ToString("0.###"),
            true,
            "numeric");
        row.ApplyWithOldNew = (oldS, newS) =>
        {
            if (!double.TryParse(oldS, out var oldV)) return;
            if (!double.TryParse(newS, out var newV)) return;
            if (Math.Abs(oldV - newV) < 0.0001) return;
            _undo.Execute(
                new PropertyChangeCommand(
                    redo: () =>
                    {
                        setter(newV);
                        RequestRender();
                    },
                    undo: () =>
                    {
                        setter(oldV);
                        RequestRender();
                    }));
        };
        PropertyRows.Add(row);
    }
    private void OnControlTransformed(DesignControl ctrl, RectMm oldRect, RectMm newRect)
    {
        _undo.Execute(
            new PropertyChangeCommand(
                redo: () =>
                {
                    ctrl.LeftMm = newRect.Left;
                    ctrl.TopMm = newRect.Top;
                    ctrl.WidthMm = newRect.Width;
                    ctrl.HeightMm = newRect.Height;
                    _logic?.UpdateControl(ctrl);
                    ShowProperties(ctrl);  // ★追加
                },
                undo: () =>
                {
                    ctrl.LeftMm = oldRect.Left;
                    ctrl.TopMm = oldRect.Top;
                    ctrl.WidthMm = oldRect.Width;
                    ctrl.HeightMm = oldRect.Height;
                    _logic?.UpdateControl(ctrl);
                    ShowProperties(ctrl);  // ★追加
                }));
    }
}