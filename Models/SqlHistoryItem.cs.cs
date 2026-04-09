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

    // ✅ ListBox表示用
    public string Summary =>
        $"{Time:yyyy-MM-dd HH:mm}  {Sql.Split('\n').FirstOrDefault()}";
}
