using System;
using System.IO;
using System.Text.Json;

namespace AcrossReportDesigner.Services;
public sealed class FullVersionInfo
{
    public DesignerVersionInfo Designer { get; set; } = new();
    public EngineVersionInfo Engine { get; set; } = new();
    public int FormatVersion { get; set; }
    public string BuildDate { get; set; } = "";
    public static FullVersionInfo Create(string engineFolder)
    {
        return new FullVersionInfo
        {
            Designer = DesignerVersionInfo.Create(),
            Engine = EngineVersionInfo.LoadFromFolder(engineFolder),
            FormatVersion = 2,
            BuildDate = DateTime.Now.ToString("yyyy-MM-dd")
        };
    }
    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
        File.WriteAllText(path, json);
    }
}
