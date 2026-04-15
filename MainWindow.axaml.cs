using AcrossReportDesigner;
using AcrossReportDesigner.Services;
using Avalonia.Controls.Templates;
using AcrossReportDesigner.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace AcrossReportDesigner.Views;

public partial class MainWindow : Window
{
    private readonly DesignerView    _designerView;
    private readonly DbConnectorView _dbConnectorView;
    private readonly ViewerView      _viewerView;

    public MainWindow()
    {
        InitializeComponent();

        _designerView    = new DesignerView();
        _dbConnectorView = new DbConnectorView();
        _viewerView      = new ViewerView();

        // ✅ DbConnectorにDesignerViewの参照を渡す（テンプレートパス取得用）
        _dbConnectorView.DesignerView = _designerView;

        MainContent.Content = _designerView;

        Icon            = new WindowIcon("Assets/app.png");
        WindowState     = WindowState.Maximized;

        // ✅ デザイナーからの再起動リクエストを購読
        _designerView.RestartRequested += OnRestartRequested;

        // ✅ Multilingual コンボ初期化
        InitLangCombo();
    }

    private void InitLangCombo()
    {
        // ✅ 言語リスト（日英のみ）
        var languages = new[]
        {
            new LanguageItem { Code = "ja", DisplayName = "日本語" },
            new LanguageItem { Code = "en", DisplayName = "English" },
        };

        LangCombo.ItemTemplate = new FuncDataTemplate<LanguageItem>(
            (item, _) => new TextBlock { Text = item?.DisplayName ?? "" });
        LangCombo.ItemsSource = languages;

        // ✅ 保存済み言語を初期選択
        var savedCode = AcrDesigner.Services.SettingsService.LoadLanguage() ?? "ja";
        var initialItem = languages.FirstOrDefault(l => l.Code == savedCode) ?? languages[0];

        // ✅ SelectionChanged を先に登録し、初回セット時は無視するフラグで制御
        bool loaded = false;
        LangCombo.SelectionChanged += (_, _) =>
        {
            if (!loaded) return;  // 初期化中は無視
            if (LangCombo.SelectedItem is not LanguageItem selected) return;
            if (selected.Code == (AcrDesigner.Services.SettingsService.LoadLanguage() ?? "ja")) return;
            AcrDesigner.Services.SettingsService.SaveLanguage(selected.Code);
            OnRestartRequested(selected.Code);
        };

        LangCombo.SelectedItem = initialItem;
        loaded = true;  // 初期化完了：以降の SelectionChanged は有効
    }

    private void Designer_Click(object? sender, RoutedEventArgs e)
        => MainContent.Content = _designerView;

    private void DataConnector_Click(object? sender, RoutedEventArgs e)
        => MainContent.Content = _dbConnectorView;

    private void DataViewer_Click(object? sender, RoutedEventArgs e)
        => MainContent.Content = _viewerView;

    private bool _exitDialogOpen = false;
    private async void Exit_Click(object? sender, RoutedEventArgs e)
    {
        if (_exitDialogOpen) return;
        _exitDialogOpen = true;
        try
        {
            var loc = LocalizationManager.Instance;
            bool result = await DialogService.ShowConfirmAsync(
                this,
                loc["Confirm_Exit"],
                loc["Confirm_Exit_Title"]);
            if (result) Close();
        }
        finally
        {
            _exitDialogOpen = false;
        }
    }

    // ======================================================
    // ✅ 言語切り替え → 再起動フロー
    //    ① 再起動しますか?
    //    ② デザイナーが開いていれば → 保存しますか?
    //    ③ 再起動
    // ======================================================
    private async void OnRestartRequested(string langCode)
    {
        // ① 再起動確認
        // ✅ 切り替え後の言語のメッセージを使用
        var loc = LocalizationManager.Instance;
        string msgKey = langCode == "en" ? "Confirm_Restart_En" : "Confirm_Restart_Ja";
        string confirmMsg = loc[msgKey];

        bool doRestart = await DialogService.ShowConfirmAsync(
            this, confirmMsg, "Language / 言語");

        if (!doRestart) return;

        // ② デザイナーにテンプレートが開いていれば保存確認
        if (_designerView.IsReportLoaded)
        {
            bool canContinue = await _designerView.ConfirmSaveBeforeRestartAsync();
            if (!canContinue) return;  // キャンセルされた → 再起動中止
        }

        // ③ アプリを再起動
        RestartApp();
    }

    private static void RestartApp()
    {
        try
        {
            // 実行中の exe パスを取得して再起動
            var exePath = Environment.ProcessPath
                ?? Process.GetCurrentProcess().MainModule?.FileName;

            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName        = exePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RESTART] ERROR: {ex.Message}");
        }
        finally
        {
            // 現在のプロセスを終了
            Environment.Exit(0);
        }
    }
}
