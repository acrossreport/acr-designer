using AcrossReportDesigner.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace AcrossReportDesigner.Services;

public static class ReportSerializer
{
    // ======================================
    // ✅ 保存
    // ======================================
    public static void Save(string path, List<ReportItem> items)
    {
        var designer = DesignerVersionInfo.Create();
        var engine = EngineVersionInfo.LoadFromFolder("engine");

        var root = new ReportFileRoot
        {
            Meta = new ReportMeta
            {
                DesignerName = designer.DesignerName,
                DesignerVersion = designer.DesignerVersion,
                EngineName = engine.EngineName,
                EngineVersion = engine.EngineVersion,
                EngineApiLevel = engine.ApiLevel,
                FormatVersion = 1
            },
            Items = items
        };

        var json = JsonSerializer.Serialize(
            root,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(path, json);
    }

    // ======================================
    // ✅ 読み込み（新形式のみ）
    // ======================================
    public static List<ReportItem> Load(string path)
    {
        var json = File.ReadAllText(path);

        var root = JsonSerializer.Deserialize<ReportFileRoot>(json);

        if (root == null || root.Items == null)
            throw new InvalidDataException("レポート形式が不正です");

        return root.Items;
    }
}
