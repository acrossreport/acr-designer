using AcrDesigner.Services;
using AcrossReportDesigner.Engines;
using AcrossReportDesigner.Models;
using AcrossReportDesigner.Rendering;
using AcrossReportDesigner.Services;
using AcrossReportDesigner.Views.Dialogs;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Views
{
    public partial class ViewerView : UserControl
    {
        private TextBox? _templatePathBox;
        private TextBox? _dataPathBox;
        private Button? _showButton;
        private Button? _pdfButton;
        private Image? _previewImage;
        private TextBox? _messageText;
        private readonly List<string> _pagePngs = new();
        // HTML 保持
        private string? _lastGeneratedHtml;
        // PDF 用
        private List<RenderNode> _lastRenderNodes = new();
        private TemplateLoadResult? _lastTemplate;
        private FontMode _embedFontMode = FontMode.NotoSans;
        // twips = 1/1440 inch
        private const double TwipsToPx = 96.0 / 1440.0;
        private const float BaselineRate = 0.82f;
        private ComboBox? _htmlModeComboBox;


        // =========================
        // ctor
        // =========================
        public ViewerView()
        {
            InitializeComponent();
            AttachedToVisualTree += OnAttached;

            Debug.WriteLine($"[ACR] DLL Version: {AcrEngine.Version}");
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        private void OnAttached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
        {
            _templatePathBox = this.FindControl<TextBox>("TemplatePathBox");
            _dataPathBox = this.FindControl<TextBox>("DataPathBox");
            _showButton = this.FindControl<Button>("ShowButton");
            _pdfButton = this.FindControl<Button>("PdfButton");
            _previewImage = this.FindControl<Image>("PreviewImage");
            _messageText = this.FindControl<TextBox>("MessageText");
            _htmlModeComboBox = this.FindControl<ComboBox>("HtmlModeComboBox");
            UpdateButtons();
        }
        // =========================
        // UI helper
        // =========================
        private void UpdateButtons()
        {
            bool okTemplate = _templatePathBox != null &&
                              !string.IsNullOrWhiteSpace(_templatePathBox.Text);
            bool okData = _dataPathBox != null &&
                          !string.IsNullOrWhiteSpace(_dataPathBox.Text);
            bool hasPages = _pagePngs.Count > 0;
            if (_showButton != null)
                _showButton.IsEnabled = okTemplate && okData;
            if (_pdfButton != null)
                _pdfButton.IsEnabled = hasPages;
        }
        private void SetMessage(string text)
        {
            if (_messageText != null)
                _messageText.Text = text;
        }
        // =========================
        // File picker
        // =========================
        private async Task<string?> PickFromFolderAsync(
            string folderName,
            string title,
            string[] patterns)
        {
            string dir = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                folderName);

            if (!Directory.Exists(dir))
            {
                await DialogService.ShowMessageAsync(
                    this,
                    $"{folderName} フォルダがありません",
                    "エラー");
                return null;
            }

            var files = patterns
                .SelectMany(p => Directory.GetFiles(dir, p))
                .Select(p => new FileInfo(p))
                .ToList();

            if (files.Count == 0)
            {
                await DialogService.ShowMessageAsync(
                    this,
                    "対象ファイルがありません",
                    title);
                return null;
            }

            var latest = files
                .OrderByDescending(f => f.LastWriteTime)
                .First();

            await DialogService.ShowMessageAsync(
                this,
                $"最新を選択しました:\n{latest.Name}",
                title);
            return latest.FullName;
        }
        private string? FindLatestFileInFolder(string folderName, string[] patterns)
        {
            string dir = System.IO.Path.Combine(AppContext.BaseDirectory, folderName);

            if (!Directory.Exists(dir))
                return null;

            var latest = patterns
                .SelectMany(p => Directory.GetFiles(dir, p))
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            return latest?.FullName;
        }
        private async void BrowseTemplate_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null) return;

            string templatePath = Path.Combine(AppContext.BaseDirectory, "Template");

            IStorageFolder? startFolder = null;
            if (Directory.Exists(templatePath))
            {
                startFolder = await owner.StorageProvider
                    .TryGetFolderFromPathAsync(templatePath);
            }

            var files = await owner.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "テンプレート(JSON)を選択",
                    AllowMultiple = false,
                    SuggestedStartLocation = startFolder,
                    FileTypeFilter = new[]
                    {
                new FilePickerFileType("Across Template")
                {
                    Patterns = new[] { "*.arc", "*.acr", "*.json" }
                }
                    }
                });

            if (files.Count == 0)
                return;

            string path = files[0].Path.LocalPath;

            var progress = new ProgressDialog();
            progress.Show(owner);

            await Task.Delay(50);

            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();

                if (ext == ".acr" || ext == ".arc")
                {
                    // ZIP形式のみ解凍処理
                    await Task.Run(() =>
                    {
                        progress.SetMessage("テンプレート解凍中...");
                        progress.SetProgress(0);

                        using var archive = ZipFile.OpenRead(path);
                        int total = archive.Entries.Count;

                        for (int i = 0; i < total; i++)
                        {
                            var entry = archive.Entries[i];

                            string outPath = Path.Combine(
                                Path.GetTempPath(),
                                entry.FullName);

                            Directory.CreateDirectory(
                                Path.GetDirectoryName(outPath)!);

                            entry.ExtractToFile(outPath, true);

                            double percent = ((i + 1) * 100.0) / total;
                            progress.SetProgress(percent);
                        }

                        progress.SetMessage("解凍完了");
                    });
                }
                // .json はそのまま（解凍不要）

                _templatePathBox!.Text = path;
                UpdateButtons();
            }
            finally
            {
                progress.Close();
            }
        }
        private async void BrowseData_Click(object? sender, RoutedEventArgs e)
        {
            var top = TopLevel.GetTopLevel(this);
            if (top == null) return;
            string dataPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Data");
            IStorageFolder? startFolder = null;
            if (Directory.Exists(dataPath))
            {
                startFolder =
                    await top.StorageProvider
                        .TryGetFolderFromPathAsync(dataPath);
            }
            var files = await top.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "データ(JSON)を選択",
                    AllowMultiple = false,
                    SuggestedStartLocation = startFolder,
                    FileTypeFilter = new[]
                    {
                new FilePickerFileType("JSON Data (*.json)")
                {
                    Patterns = new[] { "*.json" }
                }
                    }
                });
            if (files.Count == 0) return;
            _dataPathBox!.Text = files[0].Path.LocalPath;
            UpdateButtons();
        }
        // =========================
        // 表示（PNG）
        // =========================
        private async void Show_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null) return;

            var progress = new ProgressDialog();
            progress.Show(owner);
            await Task.Delay(50);

            try
            {
                _pagePngs.Clear();
                _lastRenderNodes.Clear();
                _lastTemplate = null;
                if (_previewImage != null) _previewImage.Source = null;
                SetMessage("");

                string? reportPath = _templatePathBox?.Text;
                string? dataPath   = _dataPathBox?.Text;

                if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath)) return;
                if (string.IsNullOrWhiteSpace(dataPath)   || !File.Exists(dataPath))   return;

                string savedZipPath = "";

                await Task.Run(async () =>
                {
                    // ── テンプレート読込 ──────────────────────────
                    progress.SetMessage("テンプレート読込中...");
                    progress.SetProgress(10);

                    string templateJson;
                    string ext = Path.GetExtension(reportPath).ToLowerInvariant();
                    if (ext == ".acr" || ext == ".arc")
                    {
                        using var archive = ZipFile.OpenRead(reportPath);
                        var entry = archive.Entries
                            .FirstOrDefault(e => e.FullName.EndsWith(".json",
                                StringComparison.OrdinalIgnoreCase))
                            ?? throw new Exception("ACR内にJSONが見つかりません");
                        using var stream = entry.Open();
                        using var reader = new StreamReader(stream);
                        templateJson = await reader.ReadToEndAsync();
                    }
                    else
                    {
                        templateJson = await File.ReadAllTextAsync(reportPath);
                    }

                    // ── データ読込 ────────────────────────────────
                    progress.SetMessage("データ読込中...");
                    progress.SetProgress(30);
                    string dataJson = await File.ReadAllTextAsync(dataPath);

                    // ── Rust DLL で ZIP（manifest + pages/*.png）を一括生成 ──
                    progress.SetMessage("描画中...");
                    progress.SetProgress(60);

                    var engine   = new AcrEngine();
                    byte[] zipBytes = engine.RenderZip(templateJson, dataJson);

                    // ── PngDir に yyyyMMddHHmmss.zip として保存 ───
                    progress.SetMessage("ZIP保存中...");
                    progress.SetProgress(85);

                    string pngDir = AcrConfigService.ResolvePngDir();
                    Directory.CreateDirectory(pngDir);
                    string zipName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip";
                    savedZipPath   = Path.Combine(pngDir, zipName);
                    await File.WriteAllBytesAsync(savedZipPath, zipBytes);

                    // ── プレビュー用：ZIP内の pages/*.png を一時展開 ─
                    progress.SetMessage("プレビュー準備中...");
                    progress.SetProgress(95);

                    using var ms      = new MemoryStream(zipBytes);
                    using var zipArch = new System.IO.Compression.ZipArchive(
                        ms, System.IO.Compression.ZipArchiveMode.Read);

                    var pngEntries = zipArch.Entries
                        .Where(en => en.FullName.StartsWith("pages/") &&
                                     en.FullName.EndsWith(".png"))
                        .OrderBy(en => en.FullName)
                        .ToList();

                    // 一時フォルダに展開してパスを _pagePngs に積む
                    string tempDir = Path.Combine(Path.GetTempPath(),
                        "acr_preview_" + Path.GetRandomFileName());
                    Directory.CreateDirectory(tempDir);

                    foreach (var pe in pngEntries)
                    {
                        string dest = Path.Combine(tempDir,
                            Path.GetFileName(pe.FullName));
                        pe.ExtractToFile(dest, overwrite: true);
                        _pagePngs.Add(dest);
                    }

                    progress.SetProgress(100);
                });

                if (_pagePngs.Count > 0)
                    _previewImage!.Source = new Bitmap(_pagePngs[0]);

                progress.Close();
                UpdateButtons();
                SetMessage($"表示完了：{_pagePngs.Count} ページ　ZIP保存先：{savedZipPath}");
            }
            catch (Exception ex)
            {
                progress.Close();
                SetMessage(ex.ToString());
            }
        }
        private static string ExtractValue(string json, string key)
        {
            int idx = json.IndexOf($"\"{key}\"");
            if (idx < 0) return "NOT FOUND";
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return "NO COLON";
            int end = json.IndexOfAny(new[] { ',', '}', '\n' }, colon + 1);
            return json.Substring(colon + 1, end - colon - 1).Trim();
        }
        private static async Task<TemplateLoadResult> LoadTemplateAutoAsync(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            string json;
            if (ext == ".acr" || ext == ".arc")
            {
                using var archive = System.IO.Compression.ZipFile.OpenRead(path);
                var entry = archive.Entries
                    .FirstOrDefault(e => e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                    throw new Exception("ACR内にJSONが見つかりません");
                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                json = await reader.ReadToEndAsync();
            }
            else
            {
                json = await File.ReadAllTextAsync(path);
            }
            return ParseTemplateFromJson(json);
        }
        private static TemplateLoadResult ParseTemplateFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var acr = doc.RootElement.GetProperty("ACR");
            var ps = acr.GetProperty("Report").GetProperty("PageSettings");

            double paperW = ps.GetProperty("PaperWidth").GetDouble();
            double paperH = ps.GetProperty("PaperHeight").GetDouble();

            double left = ps.GetProperty("LeftMargin").GetDouble();
            double right = ps.GetProperty("RightMargin").GetDouble();
            double top = ps.GetProperty("TopMargin").GetDouble();
            double bottom = ps.GetProperty("BottomMargin").GetDouble();

            int orientation = ps.TryGetProperty("Orientation", out var o)
                ? o.GetInt32()
                : 1;
            double pageW = paperW;
            double pageH = paperH;

            if (orientation == 2)
            {
                var tmp = pageW;
                pageW = pageH;
                pageH = tmp;
            }
            Debug.WriteLine($"PaperHeight={paperH}");
            Debug.WriteLine($"PageHeight={pageH}");
            Debug.WriteLine($"Top={top} Bottom={bottom}");

            Debug.WriteLine($"PaperHeight={paperH}");
            Debug.WriteLine($"PageHeight={pageH}");
            Debug.WriteLine($"Top={top} Bottom={bottom}");

            return new TemplateLoadResult
            {
                PageWidthTwips = pageW,
                PageHeightTwips = pageH,
                LeftMarginTwips = left,
                RightMarginTwips = right,
                TopMarginTwips = top,
                BottomMarginTwips = bottom,
                TemplateNodes = TemplateZIndexBuilder.Build(doc.RootElement)
            };
        }
        // =========================
        // PDF
        // =========================
        private async void Pdf_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string? reportPath = _templatePathBox?.Text;
                string? dataPath = _dataPathBox?.Text;
                if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
                { SetMessage("先に表示してください。"); return; }
                if (string.IsNullOrWhiteSpace(dataPath) || !File.Exists(dataPath))
                { SetMessage("先に表示してください。"); return; }

                // ✅ ファイル保存ダイアログ
                var top = TopLevel.GetTopLevel(this);
                if (top == null) return;

                string pdfDir = AcrConfigService.ResolvePdfDir();
                Directory.CreateDirectory(pdfDir);
                var initialFolder = await top.StorageProvider
                    .TryGetFolderFromPathAsync(pdfDir);

                string defaultFileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";

                var file = await top.StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = "PDFの保存先を選択",
                        SuggestedStartLocation = initialFolder,
                        SuggestedFileName = defaultFileName,
                        FileTypeChoices = new[]
                        {
                            new FilePickerFileType("PDF Files")
                            {
                                Patterns = new[] { "*.pdf" }
                            }
                        },
                        DefaultExtension = "pdf"
                    });

                // ✅ キャンセル時は何もしない
                if (file == null) return;

                string? pdfPath = file.TryGetLocalPath();
                if (string.IsNullOrEmpty(pdfPath)) return;

                string templateJson = GetTemplateJson(reportPath);
                string dataJson = File.ReadAllText(dataPath);

                var engine = new AcrEngine();
                byte[] pdf = engine.RenderPdf(templateJson, dataJson);
                File.WriteAllBytes(pdfPath, pdf);
                SetMessage($"PDF 出力完了: {pdfPath}");
            }
            catch (Exception ex) { SetMessage(ex.ToString()); }
        }
        // =========================
        // HTML 表示 / 保存
        // =========================
        private async void HtmlShow_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                bool isImageEmbed = _htmlModeComboBox?.SelectedIndex == 0;
                string html;

                if (isImageEmbed)
                {
                    var result = await BuildHtmlAsync(true);
                    if (result == null) return;
                    html = result;
                }
                else
                {
                    string? reportPath = _templatePathBox?.Text;
                    string? dataPath = _dataPathBox?.Text;  // ← 追加
                    if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
                    { SetMessage("テンプレートを選択してください。"); return; }
                    string templateJson = GetTemplateJson(reportPath);
                    string? dataJson = (!string.IsNullOrWhiteSpace(dataPath) && File.Exists(dataPath))
                        ? await File.ReadAllTextAsync(dataPath)
                        : null;
                    html = AcrHtmlEngine.RenderHtml(templateJson, dataJson ?? "");
                }
                string path = SaveHtmlTemp(html);
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                SetMessage("HTML を外部ブラウザで表示しました。");

                // ブラウザが読み込む時間を待ってから削除
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000); // 3秒待つ
                    try { File.Delete(path); } catch { }
                });
            }
            catch (Exception ex)
            {
                SetMessage(ex.ToString());
            }
        }
        private async void HtmlSave_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ ファイル保存ダイアログ
                var top = TopLevel.GetTopLevel(this);
                if (top == null) return;

                string htmlDir = AcrConfigService.ResolveHtmlDir();
                Directory.CreateDirectory(htmlDir);
                var initialFolder = await top.StorageProvider
                    .TryGetFolderFromPathAsync(htmlDir);

                string defaultFileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".html";

                var file = await top.StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = "HTMLの保存先を選択",
                        SuggestedStartLocation = initialFolder,
                        SuggestedFileName = defaultFileName,
                        FileTypeChoices = new[]
                        {
                            new FilePickerFileType("HTML Files")
                            {
                                Patterns = new[] { "*.html" }
                            }
                        },
                        DefaultExtension = "html"
                    });

                // ✅ キャンセル時は何もしない
                if (file == null) return;

                string? htmlPath = file.TryGetLocalPath();
                if (string.IsNullOrEmpty(htmlPath)) return;

                bool isImageEmbed = _htmlModeComboBox?.SelectedIndex == 0;

                if (isImageEmbed)
                {
                    // 画像埋込：HTMLだけ保存
                    var html = await BuildHtmlAsync(true);
                    if (html == null) return;
                    await File.WriteAllTextAsync(htmlPath, html, Encoding.UTF8);
                }
                else
                {
                    string? reportPath = _templatePathBox?.Text;
                    string? dataPath = _dataPathBox?.Text;
                    if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath)) return;
                    string templateJson = GetTemplateJson(reportPath);
                    string? dataJson = (!string.IsNullOrWhiteSpace(dataPath) && File.Exists(dataPath))
                        ? await File.ReadAllTextAsync(dataPath)
                        : null;
                    string html = AcrHtmlEngine.RenderHtml(templateJson, dataJson ?? "");
                    await File.WriteAllTextAsync(htmlPath, html, Encoding.UTF8);
                }
                SetMessage($"HTML 保存完了：{htmlPath}");
            }
            catch (Exception ex)
            {
                SetMessage(ex.ToString());
            }
        }
        // =========================
        // HTML 生成（安全版）
        // =========================
        private async Task<string?> BuildHtmlAsync(bool isImageEmbed = true)
        {
            string? reportPath = _templatePathBox?.Text;
            string? dataPath = _dataPathBox?.Text;
            if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath)) return null;
            if (string.IsNullOrWhiteSpace(dataPath) || !File.Exists(dataPath)) return null;
            string templateJson = GetTemplateJson(reportPath);
            string dataJson = await File.ReadAllTextAsync(dataPath);
            var engine = new AcrEngine();
            byte[] zip = engine.RenderZip(templateJson, dataJson);
            using var ms = new MemoryStream(zip);
            using var archive = new System.IO.Compression.ZipArchive(
                ms, System.IO.Compression.ZipArchiveMode.Read);

            var pngEntries = archive.Entries
                .Where(e => e.FullName.StartsWith("pages/") && e.FullName.EndsWith(".png"))
                .OrderBy(e => e.FullName)
                .ToList();

            var sb = new StringBuilder();

            if (isImageEmbed)
            {
                // 現状：SKIA→PNG→Base64埋込
                sb.AppendLine("<!DOCTYPE html><html lang='ja'><head><meta charset='utf-8'>");
                sb.AppendLine("<style>body{margin:0;background:#888;}.page{display:block;margin:8px auto;}</style>");
                sb.AppendLine("</head><body>");
                foreach (var entry in pngEntries)
                {
                    using var es = entry.Open();
                    using var buf = new MemoryStream();
                    await es.CopyToAsync(buf);
                    string b64 = Convert.ToBase64String(buf.ToArray());
                    sb.AppendLine($"<img class='page' src='data:image/png;base64,{b64}'/>");
                }
                sb.AppendLine("</body></html>");
            }
            else
            {
                // CSS形式：PNGをファイルとして参照
                sb.AppendLine("<!DOCTYPE html><html lang='ja'><head><meta charset='utf-8'>");
                sb.AppendLine("<style>body{margin:0;background:#888;}.page{display:block;margin:8px auto;}</style>");
                sb.AppendLine("</head><body>");
                foreach (var entry in pngEntries)
                {
                    string fileName = Path.GetFileName(entry.FullName);
                    sb.AppendLine($"<img class='page' src='{fileName}'/>");
                }
                sb.AppendLine("</body></html>");
            }

            return sb.ToString();
        }
        private static string GetTemplateJson(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".acr" || ext == ".arc")
            {
                using var archive = System.IO.Compression.ZipFile.OpenRead(path);
                var entry = archive.Entries
                    .FirstOrDefault(e => e.FullName.EndsWith(".json",
                        StringComparison.OrdinalIgnoreCase))
                    ?? throw new Exception("ACR内にJSONが見つかりません");
                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            return File.ReadAllText(path);
        }

        private static string SaveHtmlTemp(string html)
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "Output");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "preview.html");
            File.WriteAllText(path, html, Encoding.UTF8);
            return path;
        }
        // =========================
        // Template / Data / Render
        // =========================
        private sealed class TemplateLoadResult
        {
            public List<TemplateNode> TemplateNodes = new();
            public double PageWidthTwips;
            public double PageHeightTwips;
            public double LeftMarginTwips;
            public double RightMarginTwips;
            public double TopMarginTwips;
            public double BottomMarginTwips;
        }
        private static async Task<TemplateLoadResult> LoadTemplateAsync(string path)
        {
            string json = await File.ReadAllTextAsync(path);
            using var doc = JsonDocument.Parse(json);
            var acr = doc.RootElement.GetProperty("ACR");
            var ps = acr.GetProperty("PageSettings");
            double paperW = ps.GetProperty("PaperWidth").GetDouble();
            double paperH = ps.GetProperty("PaperHeight").GetDouble();
            int orientation = ps.TryGetProperty("Orientation", out var o) ? o.GetInt32() : 1;
            double pageW = orientation == 2 ? paperH : paperW;
            double pageH = orientation == 2 ? paperW : paperH;
            var r = new TemplateLoadResult
            {
                PageWidthTwips = pageW,
                PageHeightTwips = pageH,
                LeftMarginTwips = ps.GetProperty("LeftMargin").GetDouble(),
                RightMarginTwips = ps.GetProperty("RightMargin").GetDouble(),
                TopMarginTwips = ps.GetProperty("TopMargin").GetDouble(),
                BottomMarginTwips = ps.GetProperty("BottomMargin").GetDouble(),
            };
            r.TemplateNodes = TemplateZIndexBuilder.Build(doc.RootElement);
            return r;
        }
        private static async Task<List<Dictionary<string, string>>> LoadRowsAsync(string path)
        {
            Debug.WriteLine("### NEW VERSION LoadRowsAsync ###");
            if (Path.GetExtension(path).ToLowerInvariant() == ".csv")
                return await LoadRowsFromCsvAsync(path);
            string json = await File.ReadAllTextAsync(path);
            using var doc = JsonDocument.Parse(json);
            var list = new List<Dictionary<string, string>>();
            Debug.WriteLine("========== OUTPUT ==========");
            Debug.WriteLine($"PATH = {path}");
            Debug.WriteLine($"ROOT KIND = {doc.RootElement.ValueKind}");
            // =========================
            // Root.Parameters.Data 優先
            // =========================
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("Parameters", out var paramProp) &&
                paramProp.TryGetProperty("Data", out var dataProp) &&
                dataProp.ValueKind == JsonValueKind.Array)
            {
                Debug.WriteLine("USING ROOT.Parameters.Data ARRAY");
                int i = 0;
                foreach (var el in dataProp.EnumerateArray())
                {
                    var row = Flatten(el);
                    list.Add(row);

                    Debug.WriteLine($"-- ROW #{i++} -- COLS={row.Count}");
                }
            }
            // 配列直下JSON対応
            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                Debug.WriteLine("USING ROOT ARRAY");

                int i = 0;
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var row = Flatten(el);
                    list.Add(row);
                    Debug.WriteLine($"-- ROW #{i++} -- COLS={row.Count}");
                    foreach (var kv in row)
                        Debug.WriteLine($"{kv.Key} = {kv.Value}");
                }
            }
            // 単一オブジェクト
            else
            {
                Debug.WriteLine("USING ROOT OBJECT");
                var row = Flatten(doc.RootElement);
                list.Add(row);
                Debug.WriteLine($"-- ROW #0 -- COLS={row.Count}");
                foreach (var kv in row)
                    Debug.WriteLine($"{kv.Key} = {kv.Value}");
            }
            Debug.WriteLine($"ROW COUNT = {list.Count}");
            Debug.WriteLine("======== OUTPUT END ========");
            return list;
        }
        private static Dictionary<string, string> Flatten(JsonElement el)
        {
            var d = new Dictionary<string, string>();
            if (el.ValueKind != JsonValueKind.Object) return d;
            foreach (var p in el.EnumerateObject())
                d[p.Name] = p.Value.ToString();
            return d;
        }
        private static async Task<List<Dictionary<string, string>>> LoadRowsFromCsvAsync(string path)
        {
            var list = new List<Dictionary<string, string>>();
            var lines = await File.ReadAllLinesAsync(path);
            if (lines.Length == 0) return list;
            var header = lines[0].Split(',');
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var cols = lines[i].Split(',');
                var d = new Dictionary<string, string>();
                for (int c = 0; c < header.Length; c++)
                    d[header[c]] = c < cols.Length ? cols[c] : "";

                list.Add(d);
            }
            return list;
        }
        private static List<SKBitmap> RenderPages(
            double pageWidthTwips,
            double pageHeightTwips,
            List<RenderNode> renderNodes)
        {
            int pageWidthPx = (int)Math.Ceiling(pageWidthTwips * TwipsToPx);
            int pageHeightPx = (int)Math.Ceiling(pageHeightTwips * TwipsToPx);

            var pages = new List<SKBitmap>();

            foreach (var g in renderNodes
                                .GroupBy(n => n.PageIndex)
                                .OrderBy(g => g.Key))
            {
                var bmp = new SKBitmap(pageWidthPx, pageHeightPx);
                using var canvas = new SKCanvas(bmp);

                canvas.Clear(SKColors.White);

                foreach (var it in g.OrderBy(x => x.ZIndex))
                {
                    DrawRenderNode(canvas, it);
                }

                pages.Add(bmp);
            }

            // ★ 何も描画されなかった場合の保険
            if (pages.Count == 0)
            {
                var bmp = new SKBitmap(pageWidthPx, pageHeightPx);
                using var canvas = new SKCanvas(bmp);
                canvas.Clear(SKColors.White);
                pages.Add(bmp);
            }

            return pages;
        }

        private static void DrawRenderNode(SKCanvas c, RenderNode it)
        {
            Debug.WriteLine("=== PAGE SIZE ===");
            Debug.WriteLine($"[NODE] Text='{it.Text}'  " + $"L={it.Left} T={it.Top} W={it.Width} H={it.Height}");
            float x = (float)(it.Left * TwipsToPx);
            float y = (float)(it.Top * TwipsToPx);
            float w = (float)(it.Width * TwipsToPx);
            float h = (float)(it.Height * TwipsToPx);
            Debug.WriteLine($"       PX  x={x} y={y} w={w} h={h}");
            // ======================
            // 背景（Shape/Field）
            // ======================
            if (it.BackStyle == 1)
            {
                using var fill = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = ToColorRgb(it.BackColor),
                    IsAntialias = true
                };
                c.DrawRect(x, y, w, h, fill);
            }
            // ======================
            // 枠線（Shape/Line）
            // ======================
            if (it.LineWidth > 0)
            {
                using var border = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = Math.Max(1f, (float)(it.LineWidth * TwipsToPx)),
                    Color = ToColorRgb(it.LineColor),
                    IsAntialias = true
                };
                c.DrawRect(x, y, w, h, border);
            }
            // ======================
            // テキスト
            // ======================
                if (!string.IsNullOrWhiteSpace(it.Text))
                {
                    FontMode mode = ResolveFontModeFromFontName(
                    it.FontName,
                    FontMode.NotoSans);
                var typeface = FontManager.GetTypeface(mode);
                using var paint = new SKPaint
                {
                    Typeface = typeface,
                    TextSize = (float)(Math.Max(6.0, it.FontSize) * (96.0 / 72.0)),
                    Color = ToColorRgb(it.ForeColor),
                    IsAntialias = true,
                    SubpixelText = true
                };
                // 横位置（bounds補正）
                var bounds = new SKRect();
                paint.MeasureText(it.Text, ref bounds);
                float tx = it.TextAlign switch
                {
                    ReportTextAlign.Center => x + (w - bounds.Width) / 2 - bounds.Left,
                    ReportTextAlign.Right => x + w - bounds.Width - bounds.Left - 2,
                    _ => x - bounds.Left + 2
                };
                // 縦中央（正しい式）
                paint.GetFontMetrics(out var fm);
                Debug.WriteLine($"       FontSize={it.FontSize}  " + $"Ascent={fm.Ascent}  Descent={fm.Descent}");
                // 正しいベースライン計算
                float textHeight = fm.Descent - fm.Ascent;
                float ty = y + (h - textHeight) / 2 - fm.Ascent;
                c.DrawText(it.Text, tx, ty, paint);
            }
        }
        private static FontMode ResolveFontModeFromFontName(
            string fontName,
            FontMode fallbackMode)
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return fallbackMode;
            bool bold =
                fontName.Contains("Bold", StringComparison.OrdinalIgnoreCase) ||
                fontName.Contains("太字", StringComparison.OrdinalIgnoreCase);
            bool serif = fontName.Contains("Serif", StringComparison.OrdinalIgnoreCase);
            bool sans = fontName.Contains("Sans", StringComparison.OrdinalIgnoreCase);
            if (serif) return bold ? FontMode.NotoSerifBold : FontMode.NotoSerif;
            if (sans) return bold ? FontMode.NotoSansBold : FontMode.NotoSans;
            return fallbackMode;
        }
        private static SKColor ToColorRgb(int rgb)
        {
            byte r = (byte)((rgb >> 16) & 0xFF);
            byte g = (byte)((rgb >> 8) & 0xFF);
            byte b = (byte)(rgb & 0xFF);
            return new SKColor(r, g, b);
        }
        private static string GetOutputDir()
        {
            string dir = AcrConfigService.ResolvePngDir();
            Directory.CreateDirectory(dir);
            return dir;
        }
        private static string SavePngBytes(byte[] pngBytes)
        {
            string dir = GetOutputDir();
            string path = Path.Combine(dir,
                DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png");
            File.WriteAllBytes(path, pngBytes);
            return path;
        }
        private async Task<string?> PickFileAsync(
            string title,
            string[] patterns)
        {
            var top = TopLevel.GetTopLevel(this);
            if (top == null)
                return null;
            var files = await top.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                new FilePickerFileType(title)
                {
                    Patterns = patterns
                }
                    }
                });
            if (files.Count == 0)
                return null;
            return files[0].Path.LocalPath;
        }

        private void DoRender()
        {
            _pagePngs.Clear();
            _lastRenderNodes.Clear();
            _lastTemplate = null;

            string? reportPath = _templatePathBox?.Text;
            string? dataPath = _dataPathBox?.Text;

            if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
                throw new Exception("テンプレートが未選択");
            if (string.IsNullOrWhiteSpace(dataPath) || !File.Exists(dataPath))
                throw new Exception("データが未選択");

            // テンプレートJSON取得
            string templateJson;
            string ext = Path.GetExtension(reportPath).ToLowerInvariant();
            if (ext == ".acr" || ext == ".arc")
            {
                using var archive = ZipFile.OpenRead(reportPath);
                var entry = archive.Entries
                    .FirstOrDefault(e => e.FullName.EndsWith(".json",
                        StringComparison.OrdinalIgnoreCase))
                    ?? throw new Exception("ACR内にJSONが見つかりません");
                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                templateJson = reader.ReadToEnd();
            }
            else
            {
                templateJson = File.ReadAllText(reportPath);
            }

            string dataJson = File.ReadAllText(dataPath);

            // ★ Rust DLL で全ページ生成
            var engine = new AcrEngine();
            int pageCount = engine.GetPageCount(templateJson, dataJson);

            for (int i = 0; i < pageCount; i++)
            {
                byte[] png = engine.RenderPagePng(templateJson, dataJson, i);
                string path = SavePngBytes(png);
                _pagePngs.Add(path);
            }
        }
        private void Log(string text)
        {
            Debug.WriteLine(text);
        }

        // ★ クリアボタン：起動直後の状態に完全リセット
        private void HtmlModeClear_Click(object? sender, RoutedEventArgs e)
        {
            // テキストボックス
            if (_templatePathBox != null) _templatePathBox.Text = "";
            if (_dataPathBox     != null) _dataPathBox.Text     = "";
            if (_messageText     != null) _messageText.Text     = "";

            // プレビュー画像
            if (_previewImage != null) _previewImage.Source = null;

            // 内部状態
            _pagePngs.Clear();
            _lastRenderNodes.Clear();
            _lastTemplate        = null;
            _lastGeneratedHtml   = null;

            // ComboBox を初期値に戻す
            var combo = _htmlModeComboBox
                        ?? this.FindControl<ComboBox>("HtmlModeComboBox");
            if (combo != null) combo.SelectedIndex = 0;

            // ボタン活性状態を更新
            UpdateButtons();
        }

    }
}
