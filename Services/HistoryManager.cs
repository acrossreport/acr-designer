using AcrossReportDesigner.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AcrossReportDesigner.Services;

public static class HistoryManager
{
    private static string FilePath =
        Path.Combine(
            AppContext.BaseDirectory,
            "history",
            "history.json"
        );

    public static List<SqlHistoryItem> Load()
    {
        if (!File.Exists(FilePath))
            return new List<SqlHistoryItem>();

        string json = File.ReadAllText(FilePath);

        return JsonSerializer.Deserialize<List<SqlHistoryItem>>(json)
               ?? new List<SqlHistoryItem>();
    }

    public static void Save(List<SqlHistoryItem> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        string json = JsonSerializer.Serialize(items,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(FilePath, json);
    }
}
