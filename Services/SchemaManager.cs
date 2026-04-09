using AcrossReportDesigner.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AcrossReportDesigner.Services;

public sealed class SchemaManager
{
    private readonly Dictionary<string, ControlSchema> _schemas
        = new(StringComparer.OrdinalIgnoreCase);

    public void LoadAll(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        foreach (var file in Directory.GetFiles(folderPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);

                var schema = JsonSerializer.Deserialize<ControlSchema>(json);

                if (schema == null || string.IsNullOrWhiteSpace(schema.Name))
                    continue;

                _schemas[schema.Name] = schema;
            }
            catch
            {
                // 読み込み失敗は無視（ログ出したければここで）
            }
        }
    }

    public ControlSchema? Get(string name)
    {
        _schemas.TryGetValue(name, out var schema);
        return schema;
    }

    public IReadOnlyDictionary<string, ControlSchema> All => _schemas;
}
