// Services/AcrConfigService.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace AcrDesigner.Services;

public class AcrConfigModel
{
    public string TemplateDir { get; set; } = "template";
    public string DataDir { get; set; } = "data";
    public string PdfDir { get; set; } = "Output/PDF";
    public string PngDir { get; set; } = "Output/PNG";
    public string HtmlDir { get; set; } = "Output/html";
}

public static class AcrConfigService
{
    private const string ConfigFileName = "acrconfig.json";

    private static readonly string BaseDir =
        AppContext.BaseDirectory;

    private static readonly string ConfigFilePath =
        Path.Combine(BaseDir, ConfigFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    // ✅ 現在の設定（外部から Config.PdfDir などで参照）
    public static AcrConfigModel Config { get; private set; } = new();

    // ─── 公開メソッド ────────────────────────────────────────

    /// <summary>
    /// 起動時に呼び出す。
    /// acrconfig.json が存在しない場合はデフォルト値で新規作成する。
    /// </summary>
    public static void LoadOrCreate()
    {
        if (File.Exists(ConfigFilePath))
        {
            Load();
        }
        else
        {
            Config = new AcrConfigModel();
            Save();
            Debug.WriteLine($"[AcrConfig] 新規作成: {ConfigFilePath}");
        }

        EnsureDirectoriesExist();
    }

    /// <summary>
    /// 現在の Config を acrconfig.json に保存する
    /// </summary>
    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, JsonOptions);
            File.WriteAllText(ConfigFilePath, json, Encoding.UTF8);
            Debug.WriteLine($"[AcrConfig] 保存完了: {ConfigFilePath}");
        }
        catch
        {
            // 保存失敗は握りつぶす（SettingsServiceと同方針）
        }
    }

    // ─── パス解決（相対→絶対） ───────────────────────────────
    public static string ResolveTemplateDir() => ResolvePath(Config.TemplateDir);
    public static string ResolveDataDir() => ResolvePath(Config.DataDir);
    public static string ResolvePdfDir() => ResolvePath(Config.PdfDir);
    public static string ResolvePngDir() => ResolvePath(Config.PngDir);
    public static string ResolveHtmlDir() => ResolvePath(Config.HtmlDir);

    // ─── 非公開メソッド ──────────────────────────────────────

    private static void Load()
    {
        try
        {
            var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
            var loaded = JsonSerializer.Deserialize<AcrConfigModel>(json);
            Config = loaded ?? new AcrConfigModel();
            Debug.WriteLine($"[AcrConfig] 読み込み完了: {ConfigFilePath}");
        }
        catch (Exception ex)
        {
            // 破損時はデフォルト値で上書き保存
            Config = new AcrConfigModel();
            Save();
            Debug.WriteLine($"[AcrConfig] 読み込み失敗→デフォルト再作成: {ex.Message}");
        }
    }

    private static void EnsureDirectoriesExist()
    {
        CreateIfNotExists(ResolveTemplateDir());
        CreateIfNotExists(ResolveDataDir());
        CreateIfNotExists(ResolvePdfDir());
        CreateIfNotExists(ResolvePngDir());
        CreateIfNotExists(ResolveHtmlDir());
    }

    private static void CreateIfNotExists(string absolutePath)
    {
        if (!Directory.Exists(absolutePath))
        {
            Directory.CreateDirectory(absolutePath);
            Debug.WriteLine($"[AcrConfig] フォルダ作成: {absolutePath}");
        }
    }

    private static string ResolvePath(string path)
        => Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(BaseDir, path));
}