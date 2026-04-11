using AcrDesigner.Services;
using AcrossReportDesigner.Resources;
using Avalonia.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;

namespace AcrossReportDesigner;

public class LocalizationManager : INotifyPropertyChanged
{
    // ✅ シングルトン（App.axaml.cs でリソースに登録される唯一のインスタンス）
    public static LocalizationManager Instance { get; } = new();

    private ResourceManager _rm = new ResourceManager(
        typeof(AcrossReportDesigner.Resources.Strings));

    public event PropertyChangedEventHandler? PropertyChanged;

    // ✅ インデクサ（axaml の {Binding [Key]} で参照される）
    public string this[string key] =>
        _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public void SwitchLanguage(string langCode)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(langCode);
        CultureInfo.CurrentCulture   = new CultureInfo(langCode);

        Debug.WriteLine($"[LANG] Switched to {langCode} → Toolbar_New={this["Toolbar_New"]}");

        // ✅ UIスレッドで PropertyChanged を発火
        //    "Item[]" は Avalonia のインデクサバインディング更新トリガー
        Dispatcher.UIThread.Post(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        });

        SettingsService.SaveLanguage(langCode);
    }
}
