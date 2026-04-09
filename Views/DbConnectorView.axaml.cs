using AcrossReportDesigner.Data;
using AcrossReportDesigner.Data.Providers;
using AcrossReportDesigner.Models;
using AcrossReportDesigner.Services;

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
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

        HistoryComboBox.SelectionChanged += HistoryComboBox_SelectionChanged;
        ReloadHistoryDropdown();

        // ✅ 初期接続文字列（例）
        ConnectionStringTextBox.Text =
            "user id=miuraya;password=miuraya;data source=" +
            "(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST=192.168.33.45)(PORT=1521))" +
            "(CONNECT_DATA=(SERVICE_NAME=acm50.sunfood)))";

        RefreshParameterGrid();
    }

    private void HistoryComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int index = HistoryComboBox.SelectedIndex;

        // ✅ 先頭（案内）は無視
        if (index <= 0)
            return;

        // ✅ 選ばれた履歴
        var item = _historyCache[index - 1];

        // ✅ SQL復元
        ConnectionStringTextBox.Text = item.Connection;
        SqlTextBox.Text = item.Sql;

        // ✅ パラメータ復元
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


    // =====================================================
    // ✅ 接続確認
    // =====================================================
    private async void Connect_Click(object? sender, RoutedEventArgs e)
    {
        ExecuteButton.IsEnabled = true;
        await DialogService.ShowMessageAsync(this, "接続設定を確認しました", "情報");
    }

    private async void Execute_Click(object? sender, RoutedEventArgs e)
    {
        await ExecuteSqlAsync();
    }

    private void Clear_Click(object? sender, RoutedEventArgs e)
    {
        SqlTextBox.Text = string.Empty;

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
                _ => throw new NotSupportedException()
            };
            // ✅ 入力済みパラメータだけ辞書化
            var paramDict = ParamList
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
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
            // ✅ 履歴ドロップダウン更新
            ReloadHistoryDropdown();
            // ✅ PreviewGrid表示用に変換
            var list = table.AsEnumerable()
                .Select(row =>
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (DataColumn col in table.Columns)
                    {
                        object value = row[col];
                        dict[col.ColumnName] =
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
                string columnName = col.ColumnName;

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
                "SQL実行エラー"
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
    private void ReloadHistoryDropdown()
    {
        _historyCache = HistoryManager.Load();
        HistoryComboBox.ItemsSource =
            _historyCache.Select(x =>
                $"{x.Time:MM/dd HH:mm} {(x.Sql)}"
            ).ToList();
    }
    // =====================================================
    // ✅ JSON Export（空欄パラメータ除外）
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
            object jsonObj = new
            {
                Parameters = paramDict,
                Sql = sql,
                Data = dataRows
            };
            string json = JsonSerializer.Serialize(
                jsonObj,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            // =========================
            // Dataフォルダ固定
            // =========================
            string dataDir = Path.Combine(
                AppContext.BaseDirectory,
                "Data");
            Directory.CreateDirectory(dataDir);
            string fileName =
                "AcrData_" +
                DateTime.Now.ToString("yyyyMMddHHmmss") +
                ".json";
            string fullPath = Path.Combine(dataDir, fileName);

            File.WriteAllText(fullPath, json);
            await DialogService.ShowMessageAsync(
                this,
                $"JSONを出力しました\n{fileName}",
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
