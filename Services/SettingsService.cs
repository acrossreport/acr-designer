// Services/SettingsService.cs
using System;
using System.IO;
using System.Text.Json;

namespace AcrDesigner.Services;

public class AppSettings
{
    public string? Language { get; set; }
    // 今後の設定項目はここに追加
    // 例: public string? LastOpenedFile { get; set; }
}

public static class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AcrDesigner");

    private static readonly string SettingsPath = Path.Combine(
        SettingsDir, "acr_designer_settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    // ✅ 言語設定を読み込む（ファイルがなければnull）
    public static string? LoadLanguage()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return null;

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings?.Language;
        }
        catch
        {
            return null; // 読めなければOSフォールバック
        }
    }

    // ✅ 言語設定を保存する
    public static void SaveLanguage(string langCode)
    {
        try
        {
            // 既存設定を読み込んでマージ（他の設定を上書きしない）
            var settings = LoadAll() ?? new AppSettings();
            settings.Language = langCode;
            Save(settings);
        }
        catch
        {
            // 保存失敗は握りつぶす（致命的ではない）
        }
    }

    // ✅ 設定全体を読み込む
    private static AppSettings? LoadAll()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return null;
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json);
        }
        catch
        {
            return null;
        }
    }

    // ✅ 設定全体を保存する
    private static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir); // なければ作成
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}