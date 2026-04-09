using AcrossReportDesigner.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AcrossReportDesigner.Services;

public static class PropertyGridBuilder
{
    public static List<PropertyRow> BuildRows(
        string path,
        DesignControl ctrl,
        IEnumerable<string>? fontNames = null)
    {
        if (!File.Exists(path))
            return new List<PropertyRow>();

        var json = File.ReadAllText(path);

        var schema = JsonSerializer.Deserialize<SchemaProperty>(json);
        if (schema == null)
            return new List<PropertyRow>();

        var rows = new List<PropertyRow>();

        foreach (var def in schema.Properties)
        {
            // def.Key / def.Label / def.Editor を使う（あなたのSchema定義に合わせる）
            var prop = ctrl.GetType().GetProperty(def.Key);
            var val = prop?.GetValue(ctrl)?.ToString() ?? "";

            var row = new PropertyRow(
                def.Key,
                val,
                true,
                string.IsNullOrWhiteSpace(def.Editor) ? "textbox" : def.Editor
            );

            if (row.Editor == "fontpicker" && fontNames != null)
            {
                row.ComboItems = fontNames.ToList();
            }

            rows.Add(row);
        }

        return rows;
    }
}
