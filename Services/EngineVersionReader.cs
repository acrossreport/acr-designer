using System.IO;
using System.Text.Json;

namespace AcrossReportDesigner.Services;

public sealed class EngineVersionInfo
{
    public string EngineName { get; set; } = "";
    public string EngineVersion { get; set; } = "";
    public int ApiLevel { get; set; }
    // =====================================
    // ✅ エンジンフォルダから version.json 読み込み
    // =====================================
    public static EngineVersionInfo LoadFromFolder(string engineFolder)
    {
        try
        {
            var path = Path.Combine(engineFolder, "version.json");

            if (!File.Exists(path))
            {
                return new EngineVersionInfo
                {
                    EngineName = "not-installed",
                    EngineVersion = "unknown",
                    ApiLevel = 0
                };
            }
            var text = File.ReadAllText(path);
            var info = JsonSerializer.Deserialize<EngineVersionInfo>(text);
            if (info == null)
            {
                return new EngineVersionInfo
                {
                    EngineName = "invalid-version-file",
                    EngineVersion = "unknown",
                    ApiLevel = 0
                };
            }
            return info;
        }
        catch
        {
            return new EngineVersionInfo
            {
                EngineName = "read-error",
                EngineVersion = "unknown",
                ApiLevel = 0
            };
        }
    }
}
