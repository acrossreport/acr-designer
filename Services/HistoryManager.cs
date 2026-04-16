using AcrossReportDesigner.Models;
using AcrDesigner.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AcrossReportDesigner.Services;

public static class HistoryManager
{
    /// <summary>
    /// SqlHist フォルダ内の *_sql.json を新しい順に読み込む
    /// </summary>
    public static List<SqlHistoryItem> Load()
    {
        string histDir = AcrConfigService.ResolveSqlDir();

        if (!Directory.Exists(histDir))
            return new List<SqlHistoryItem>();

        var files = Directory
            .GetFiles(histDir, "*_sql.json")
            .OrderByDescending(f => f)  // ファイル名降順 = 新しい順
            .ToList();

        var items = new List<SqlHistoryItem>();

        foreach (var file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                var node = JsonNode.Parse(json);
                if (node == null) continue;

                var item = new SqlHistoryItem();

                // ExecutedAt → Time
                string? executedAt = node["ExecutedAt"]?.GetValue<string>();
                if (DateTime.TryParse(executedAt, out var dt))
                    item.Time = dt;

                // DbType → DbType
                item.DbType = node["DbType"]?.GetValue<string>() ?? "";

                // ConnectionString → Connection
                item.Connection = node["ConnectionString"]?.GetValue<string>() ?? "";

                // Sql → Sql
                item.Sql = node["Sql"]?.GetValue<string>() ?? "";

                // Parameters → Parameters
                var paramNode = node["Parameters"];
                if (paramNode is JsonObject paramObj)
                {
                    foreach (var kv in paramObj)
                        item.Parameters[kv.Key] = kv.Value?.GetValue<string>() ?? "";
                }

                items.Add(item);
            }
            catch
            {
                // 破損ファイルはスキップ
            }
        }

        return items;
    }

    /// <summary>
    /// リスト受け取り版（呼び出し側互換）
    /// ※ 実際の保存は SaveSqlLog 側の個別ファイルで行っているため、
    ///    ここでは何もしない（SaveSqlLog との二重保存を防止）
    /// </summary>
    public static void Save(List<SqlHistoryItem> items)
    {
        // SaveSqlLog が個別ファイル保存を担っているため不要
        // 呼び出し側のコンパイルエラー回避のためシグネチャのみ残す
    }
}
