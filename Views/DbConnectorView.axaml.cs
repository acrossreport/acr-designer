using AcrDesigner.Services;
using AcrossReportDesigner.Data;
using AcrossReportDesigner.Data.Providers;
using AcrossReportDesigner.Models;
using AcrossReportDesigner.Services;

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Views;

public partial class DbConnectorView : UserControl
{
    // ✅ SQL入力 Debounce
    private DispatcherTimer? _sqlTimer;

    // ✅ 動的パラメータリスト（横1列）
    public ObservableCollection<SqlParam> ParamList { get; } = new();

    private List<SqlHistoryItem> _historyCache = new();

    // ✅ デザイナービュー参照（テンプレートファイルパス取得用）
    public DesignerView? DesignerView { get; set; }

    public DbConnectorView()
    {
        InitializeComponent();

        ExecuteButton.IsEnabled = false;

        // ✅ ParamItems 初期化
        SetupParameterGrid();

        // ✅ Debounce Timer
        _sqlTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };

        _sqlTimer.Tick += (_, _) =>
        {
            _sqlTimer.Stop();
            RefreshParameterGrid();
        };

        // ✅ SQL変更時にパラメータ欄更新
        SqlTextBox.TextChanged += (_, _) =>
        {
            _sqlTimer.Stop();
            _sqlTimer.Start();
        };

        //ConnectionStringTextBox.Text =
        //    "User Id=miuraya;" +
        //    "Password=miuraya;" +
        //    "Data Source=192.168.33.45:1521/acm50.sunfood;" +
        //    "HA Events=false;" +
        //    "Pooling=false;";

        //ConnectionStringTextBox.Text =
        //    "Server=localhost\\SQLEXPRESS,61854;" +
        //    "Database=acm;" +
        //    "User Id=sa;" +
        //    "Password=Across5567;" +
        //    "TrustServerCertificate=True;";

        //ConnectionStringTextBox.Text =
        //    "Host=localhost;" +
        //    "Port=5432;" +
        //    "Database=acm;" +
        //    "Username=postgres;" +
        //    "Password=Across5567;";

        //ConnectionStringTextBox.Text =
        //    "Server=localhost;" +
        //    "Port=3306;" +
        //    "Database=across;" +
        //    "User Id=root;" +
        //    "Password=Across5567;";

        //ConnectionStringTextBox.Text =
        //    "Provider=Microsoft.ACE.OLEDB.12.0;" +
        //    "Data Source=C:\\path\\to\\Northwind.accdb;";
        // ✅ DB種別変更時のダイアログ制御
        DbTypeComboBox.SelectionChanged += DbType_SelectionChanged;
        
        RefreshParameterGrid();
    }

    // =====================================================
    // ✅ DB種別変更 → SQLite/CSV はファイル選択ダイアログ
    // =====================================================
    private async void DbType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int index = DbTypeComboBox.SelectedIndex;

        // SQLite = 4, Access = 5
        if (index == 4)
        {
            await PickFileForConnectionAsync(
                title: "SQLiteファイルを選択",
                filters: new[] { ("SQLite Files", new[] { "db", "sqlite", "sqlite3" }) },
                buildConnStr: path => $"Data Source={path}"
            );
        }
        else if (index == 5) // Access
        {
            await PickFileForConnectionAsync(
                title: "Accessファイルを選択",
                filters: new[] { ("Access Files", new[] { "accdb", "mdb" }) },
                buildConnStr: path => $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};"
            );
        }
    }

    private async Task PickFileForConnectionAsync(
        string title,
        (string Name, string[] Extensions)[] filters,
        Func<string, string> buildConnStr)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        // ✅ 初期フォルダ = acrconfig.json の DataDir
        var initialDir = AcrConfigService.ResolveDataDir();
        var initialFolder = await topLevel.StorageProvider
            .TryGetFolderFromPathAsync(initialDir);

        var fileTypeFilters = filters
            .Select(f => new Avalonia.Platform.Storage.FilePickerFileType(f.Name)
            {
                Patterns = f.Extensions.Select(ext => $"*.{ext}").ToArray()
            })
            .ToList();

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = fileTypeFilters,
                SuggestedStartLocation = initialFolder
            });
        if (files.Count == 0) return; // キャンセル
        var path = files[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(path)) return;
        ConnectionStringTextBox.Text = buildConnStr(path);
    }
    // =====================================================
    // ✅ 接続確認 - 実際にDB接続テストを行う
    // =====================================================
    private async void Connect_Click(object? sender, RoutedEventArgs e)
    {
        // ✅ 接続文字列チェック
        if (string.IsNullOrWhiteSpace(ConnectionStringTextBox.Text))
        {
            await DialogService.ShowMessageAsync(this, "接続文字列が入力されていません。", "エラー");
            return;
        }

        // ✅ SQLチェック
        if (string.IsNullOrWhiteSpace(SqlTextBox.Text))
        {
            await DialogService.ShowMessageAsync(this, "SQLが入力されていません。", "エラー");
            return;
        }

        // ✅ パラメータ値チェック
        var emptyParams = ParamList.Where(p => string.IsNullOrWhiteSpace(p.Value)).ToList();
        if (emptyParams.Any())
        {
            string names = string.Join(", ", emptyParams.Select(p => p.Name));
            await DialogService.ShowMessageAsync(this, $"パラメータの値が入力されていません。\n{names}", "エラー");
            return;
        }

        // ✅ ボタン無効化（二重クリック防止）
        ConnectButton.IsEnabled = false;
        ExecuteButton.IsEnabled = false;

        try
        {
            var dbType = (AcrDbType)DbTypeComboBox.SelectedIndex;
            string connStr = ConnectionStringTextBox.Text.Trim();

            // ✅ 全DB共通で非同期接続テスト
            await DbConnectionFactory.TestConnectionAsync(dbType, connStr);

            ExecuteButton.IsEnabled = true;
            await DialogService.ShowMessageAsync(this, "接続に成功しました。", "接続確認");
        }
        catch (Exception ex)
        {
            ExecuteButton.IsEnabled = false;
            await DialogService.ShowMessageAsync(this, $"接続に失敗しました。\n{ex.Message}", "接続エラー");
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private async void Execute_Click(object? sender, RoutedEventArgs e)
    {
        await ExecuteSqlAsync();
    }

    private void Clear_Click(object? sender, RoutedEventArgs e)
    {
        SqlTextBox.Text = string.Empty;
         ConnectionStringTextBox.Text = string.Empty;  // ✅ 追加

        PreviewGrid.ItemsSource = null;
        PreviewGrid.Columns.Clear();

        ExecuteButton.IsEnabled = false;

        // ✅ パラメータも初期化
        ParamList.Clear();
        ParamItems.ItemsSource = null;
        ParamItems.ItemsSource = ParamList;
    }

    private async void History_Click(object? sender, RoutedEventArgs e)
    {
        var list = HistoryManager.Load();

        if (list.Count == 0)
            return;

        // ✅ 履歴画面を開く
        var win = new HistoryView(list);

        // ✅ DbConnectorView は UserControl なので親Windowを取る
        var parent = this.GetVisualRoot() as Window;
        if (parent == null)
            return;

        // ✅ Windowを親にして表示する
        await win.ShowDialog(parent);

        // ✅ 選択されたものを復元
        if (win.SelectedItem == null)
            return;

        var item = win.SelectedItem;

        ConnectionStringTextBox.Text = item.Connection;
        SqlTextBox.Text = item.Sql;

        ParamList.Clear();
        foreach (var kv in item.Parameters)
        {
            ParamList.Add(new SqlParam
            {
                Name = kv.Key,
                Value = kv.Value
            });
        }
    }

    private void AddParam_Click(object? sender, RoutedEventArgs e)
    {
        // ✅ 新しいパラメータ追加
        var param = new SqlParam
        {
            Name = "NEW_PARAM",
            Value = ""
        };

        ParamList.Add(param);

        // ✅ 再バインド
        ParamItems.ItemsSource = null;
        ParamItems.ItemsSource = ParamList;

        // ✅ 少し遅延させてフォーカス移動
        Dispatcher.UIThread.Post(() =>
        {
            FocusLastParamValueBox();
        });
    }

    private void FocusLastParamValueBox()
    {
        if (ParamItems.ItemCount == 0)
            return;

        var lastIndex = ParamItems.ItemCount - 1;

        var container = ParamItems.ContainerFromIndex(lastIndex) as Control;
        if (container == null)
            return;

        // ✅ 最後の行の TextBox を探す
        var boxes = container
            .GetVisualDescendants()
            .OfType<TextBox>()
            .ToList();

        if (boxes.Count >= 1)
        {
            boxes[0].Focus();
            boxes[0].SelectAll();
        }
    }


    // =====================================================
    // ✅ Enterで右へ移動（横パラメータ欄）
    // =====================================================
    private void ParamBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        // ✅ ParamItems配下のTextBoxだけ取得
        var boxes =
            this.GetVisualDescendants()
                .OfType<TextBox>()
                .Where(x => x.Tag is SqlParam)
                .ToList();

        var current = (TextBox)sender!;
        int index = boxes.IndexOf(current);

        if (index < 0) return;

        int next = (index + 1) % boxes.Count;
        boxes[next].Focus();
    }

    // =====================================================
    // ✅ SQL実行（ParamItems対応）
    // =====================================================
    private async Task ExecuteSqlAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"★SQL=[{SqlTextBox.Text!}]");
            System.Diagnostics.Debug.WriteLine($"★CONN=[{ConnectionStringTextBox.Text!}]");
            if (string.IsNullOrWhiteSpace(SqlTextBox.Text))
            {
                await DialogService.ShowMessageAsync(
                    this,
                    "SQL が入力されていません。",
                    "エラー");
                return;
            }
            // ✅ DB種別選択
            IDataSource source = DbTypeComboBox.SelectedIndex switch
            {
                0 => new OracleSource(),
                1 => new SqlServerSource(),
                2 => new PostgreSqlSource(),
                3 => new MySqlSource(),
                4 => new SQLiteSource(),
                5 => new AccessSource(),
                _ => throw new NotSupportedException()
            };
            // ✅ 空白パラメータチェック
            var emptyParams = ParamList
                .Where(x => string.IsNullOrWhiteSpace(x.Value))
                .Select(x => x.Name)
                .ToList();

            if (emptyParams.Count > 0)
            {
                await DialogService.ShowMessageAsync(
                    this,
                    $"パラメータが不足しています。\n未入力: {string.Join(", ", emptyParams)}",
                    "エラー");
                return;
            }

            // ✅ パラメータ辞書化（全件確定済み）
            var paramDict = ParamList
                .ToDictionary(
                    x => x.Name,
                    x => (object?)x.Value.Trim()
                );
            // ✅ SQL実行
            DataTable table = await source.ExecuteAsync(
                ConnectionStringTextBox.Text!,
                SqlTextBox.Text!,
                paramDict
            );
            // ✅ 実行成功したら履歴保存する
            SaveToHistory();
            // ✅ SqlDirにSQL実行ログを保存
            SaveSqlLog(SqlTextBox.Text ?? "");
            // ✅ PreviewGrid表示用に変換
            var list = table.AsEnumerable()
                .Select(row =>
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (DataColumn col in table.Columns)
                    {
                        object value = row[col];
                        dict[col.ColumnName.ToUpperInvariant()] =
                            value == DBNull.Value ? null : value;
                    }

                    return dict;
                })
                .ToList();

            // ✅ DataGrid更新
            PreviewGrid.ItemsSource = null;
            PreviewGrid.Columns.Clear();

            PreviewGrid.AutoGenerateColumns = false;

            foreach (DataColumn col in table.Columns)
            {
                string columnName = col.ColumnName.ToUpperInvariant();

                PreviewGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = columnName,
                    Binding = new Binding($"[{columnName}]")
                });
            }
            PreviewGrid.ItemsSource = list;
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(
                this,
                ex.Message,
                "SQLエラー"
            );
        }
    }
    private void SaveToHistory()
    {
        var list = HistoryManager.Load();
        list.Insert(0, new SqlHistoryItem
        {
            Time = DateTime.Now,
            DbType = DbTypeComboBox.SelectedIndex.ToString(),
            Connection = ConnectionStringTextBox.Text ?? "",
            Sql = SqlTextBox.Text ?? "",
            Parameters = ParamList.ToDictionary(
                p => p.Name,
                p => p.Value
            )
        });
        if (list.Count > 30)
            list = list.Take(30).ToList();
        HistoryManager.Save(list);
    }

    // =====================================================
    // ✅ SQL実行ログを SqlDir に保存
    //    ファイル名: yyyyMMddHHmmss_sql.json
    //    内容: 実行日時 + SQL全文
    // =====================================================

    // =====================================================
    // ✅ SQL実行ログを SqlDir に保存
    //    ファイル名: yyyyMMddHHmmss_sql.json
    //    内容: 実行日時 + 接続情報 + SQL全文
    // =====================================================
    private void SaveSqlLog(string sql)
    {
        try
        {
            string sqlDir = AcrConfigService.ResolveSqlDir();
            Directory.CreateDirectory(sqlDir);

            string fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + "_sql.json";
            string filePath = Path.Combine(sqlDir, fileName);

            var logObj = new
            {
                ExecutedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DbType = DbTypeComboBox.SelectedIndex.ToString(),
                ConnectionString = ConnectionStringTextBox.Text ?? "",   // ✅ 追加
                Sql = sql,
                Parameters = ParamList.ToDictionary(p => p.Name, p => p.Value)
            };

            string json = JsonSerializer.Serialize(
                logObj,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SqlLog] 保存失敗: {ex.Message}");
        }
    }
    // =====================================================
    // ✅ JSON Export（ファイル保存ダイアログ）
    // =====================================================
    private async void ExportJson_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string sql = SqlTextBox.Text ?? "";
            var paramDict = ParamList
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .ToDictionary(
                    x => x.Name,
                    x => x.Value.Trim()
                );
            var dataRows = new List<Dictionary<string, object?>>();
            if (PreviewGrid.ItemsSource is IEnumerable<Dictionary<string, object?>> items)
            {
                dataRows = items.ToList();
            }

            // ✅ テンプレート情報（デザイナで開いているファイル）
            string templatePath = DesignerView?.CurrentTemplatePath ?? "";
            string templateFile = string.IsNullOrEmpty(templatePath)
                ? ""
                : Path.GetFileName(templatePath);

            object jsonObj = new
            {
                Meta = new
                {
                    CreatedAt    = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TemplateFile = templateFile,
                    TemplatePath = templatePath
                },
                Parameters = paramDict,
                Data = dataRows
            };
            string json = JsonSerializer.Serialize(
                jsonObj,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            // ✅ ファイル保存ダイアログ
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            // ✅ 初期フォルダ = acrconfig.json の DataDir
            string dataDir = AcrConfigService.ResolveDataDir();
            Directory.CreateDirectory(dataDir);
            var initialFolder = await topLevel.StorageProvider
                .TryGetFolderFromPathAsync(dataDir);

            // ✅ 初期ファイル名
            string defaultFileName =
                "AcrData_" +
                DateTime.Now.ToString("yyyyMMddHHmmss") +
                ".json";

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "JSONファイルの保存先を選択",
                    SuggestedStartLocation = initialFolder,
                    SuggestedFileName = defaultFileName,
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                        {
                            Patterns = new[] { "*.json" }
                        }
                    },
                    DefaultExtension = "json"
                });

            // ✅ キャンセル時は何もしない
            if (file == null) return;

            var fullPath = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(fullPath)) return;

            File.WriteAllText(fullPath, json);

            await DialogService.ShowMessageAsync(
                this,
                $"JSONを出力しました\n{Path.GetFileName(fullPath)}",
                "出力完了");
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(
                this,
                ex.Message,
                "エラー");
        }
    }
    // =====================================================
    // ✅ ParamItems 初期化
    // =====================================================
    private void SetupParameterGrid()
    {
        ParamItems.ItemsSource = ParamList;
    }

    // =====================================================
    // ✅ SQLからParameter抽出 → ParamItems生成
    // =====================================================
    private void RefreshParameterGrid()
    {
        string sql = SqlTextBox.Text ?? "";
        var names = ExtractParameters(sql);

        Debug.WriteLine("SQL = " + sql);
        Debug.WriteLine("PARAM COUNT = " + names.Count);

        foreach (var n in names)
        {
            Debug.WriteLine("PARAM = " + n);
        }

        ParamList.Clear();

        foreach (var raw in names)
        {
            ParamList.Add(new SqlParam
            {
                Name = raw.TrimStart(':', '@'),
                Value = ""
            });
        }

        // ✅ ItemsControl再描画
        ParamItems.ItemsSource = null;
        ParamItems.ItemsSource = ParamList;

        // ✅ 無ければ非表示
        ParamItems.IsVisible = ParamList.Count > 0;
    }


    // =====================================================
    // ✅ :AAA / @AAA 抽出（出現順）
    // =====================================================
    private static List<string> ExtractParameters(string sql)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();

        var matches = Regex.Matches(sql, @"[:@][A-Za-z0-9_]+");

        foreach (Match m in matches)
        {
            string p = m.Value;
            if (seen.Add(p))
                result.Add(p);
        }

        return result;
    }
}
