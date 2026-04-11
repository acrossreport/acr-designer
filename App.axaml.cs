// App.axaml.cs
using AcrDesigner.Services;
using AcrDesigner.ViewModels;
using AcrDesigner.Views;
using AcrossReportDesigner.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AcrossReportDesigner;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // XAMLで生成された別インスタンスをシングルトンに差し替え
        Resources["Loc"] = LocalizationManager.Instance;
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        // ✅ 言語設定（最初に適用）
        var supported = new[] { "ja", "en", "zh", "ko" };
        var saved = SettingsService.LoadLanguage();
        var osLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var langCode = saved ?? (supported.Contains(osLang) ? osLang : "ja");
        LocalizationManager.Instance.SwitchLanguage(langCode);

        // ✅ ACR設定の読み込み（なければ新規作成）← ここに追加
        AcrConfigService.LoadOrCreate();

        // ✅ OS別フォントの設定
        Resources["DefaultFontFamily"] = GetOsFontFamily();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var licenseService = new LicenseService();
            var check = await licenseService.CheckAsync();
            bool watermark = check.Watermark;
            Debug.WriteLine($"[LICENSE] IsLicensed={check.IsLicensed} Watermark={check.Watermark} IsExpired={check.IsExpired}");

            // MainWindowを先に作る
            var mainVm = new AcrossReportDesigner.ViewModels.MainWindowViewModel(licenseService, watermarkEnabled: watermark);
            var mainWindow = new MainWindow { DataContext = mainVm };
            desktop.MainWindow = mainWindow;

            if (check.IsExpired)
            {
                var dlgVm = new LicenseDialogViewModel(licenseService, canSkip: false);
                var dlg = new LicenseDialog(dlgVm);
                mainWindow.Show();
                var licensed = await dlg.ShowDialog<bool>(mainWindow);
                if (!licensed)
                {
                    desktop.Shutdown();
                    return;
                }
                watermark = false;
            }
            else if (!check.IsLicensed)
            {
                var dlgVm = new LicenseDialogViewModel(licenseService, canSkip: true);
                var dlg = new LicenseDialog(dlgVm);
                mainWindow.Show();
                var licensed = await dlg.ShowDialog<bool>(mainWindow);
                watermark = !licensed;
            }
            else
            {
                mainWindow.Show();
            }
        }
    }
    private static Avalonia.Media.FontFamily GetOsFontFamily()
    {
        if (OperatingSystem.IsWindows())
            return new Avalonia.Media.FontFamily("Yu Gothic UI, Meiryo, MS UI Gothic");
        if (OperatingSystem.IsMacOS())
            return new Avalonia.Media.FontFamily("Hiragino Sans, Hiragino Kaku Gothic ProN");
        return new Avalonia.Media.FontFamily("Noto Sans CJK JP, IPAexGothic");
    }
}