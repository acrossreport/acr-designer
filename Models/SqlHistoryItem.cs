using System;
using System.Collections.Generic;
using System.Linq;

namespace AcrossReportDesigner.Models;

public sealed class SqlHistoryItem
{
    public DateTime Time { get; set; }

    public string DbType { get; set; } = "";

    public string Connection { get; set; } = "";

    public string Sql { get; set; } = "";

    public Dictionary<string, string> Parameters { get; set; }
        = new();

    // ✅ DB種別名（XAMLコンボボックスの順番に合わせる）
    public string DbTypeName => DbType switch
    {
        "0" => "Oracle",
        "1" => "SQL Server",
        "2" => "PostgreSQL",
        "3" => "MySQL",
        "4" => "SQLite",
        "5" => "Access",
        _   => string.IsNullOrEmpty(DbType) ? "" : $"DB({DbType})"
    };

    // ✅ 1行目: 日時 + DB種別
    public string SummaryLine1 =>
        $"{Time:yyyy-MM-dd HH:mm}  [{DbTypeName}]";

    // ✅ 2行目: 接続情報
    public string SummaryLine2 =>
        Connection.Length > 80
            ? Connection[..80] + "…"
            : Connection;

    // ✅ 3行目: SQL先頭100文字
    public string SummaryLine3
    {
        get
        {
            string sqlHead = Sql
                .Replace("\r\n", " ")
                .Replace("\r",   " ")
                .Replace("\n",   " ")
                .Replace("\t",   " ")
                .Trim();

            while (sqlHead.Contains("  "))
                sqlHead = sqlHead.Replace("  ", " ");

            return sqlHead.Length > 100
                ? sqlHead[..100] + "…"
                : sqlHead;
        }
    }

    // ✅ 互換用
    public string Summary => $"{SummaryLine1}\n{SummaryLine2}\n{SummaryLine3}";
}
