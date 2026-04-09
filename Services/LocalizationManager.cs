using AcrDesigner.Services;
using AcrossReportDesigner.Resources;
using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;

namespace AcrossReportDesigner;

public class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    private ResourceManager _rm = new ResourceManager(
        typeof(AcrossReportDesigner.Resources.Strings)); // 自動生成されたStrings.Designer.cs

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] =>
        _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public void SwitchLanguage(string langCode)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(langCode);
        CultureInfo.CurrentCulture = new CultureInfo(langCode);
        Debug.WriteLine($"[LANG] Switched to {langCode}, test key: {this["Toolbar_New"]}");
        // 全バインディングに更新を通知
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        // 設定ファイルに保存
        SettingsService.SaveLanguage(langCode);
    }
}