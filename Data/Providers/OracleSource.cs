using AcrossReportDesigner.Data;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Data.Providers;

public sealed class OracleSource : IDataSource
{
    public async Task<DataTable> ExecuteAsync(
        string connStr,
        string sql,
        Dictionary<string, object?> paramDict)
    {
        // ✅ ONS無効化（RAC未使用環境でのORA-50082回避）
        if (!connStr.Contains("HA Events", StringComparison.OrdinalIgnoreCase))
            connStr += ";HA Events=false";

        // ✅ 末尾セミコロン除去（ORA-00936対策）
        sql = sql.TrimEnd(';', ' ', '\r', '\n');

        // ✅ @PARAM → :PARAM 変換（Oracle形式に統一）
        sql = Regex.Replace(sql, @"@([A-Za-z0-9_]+)", ":$1");

        using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 120;
        cmd.BindByName = true;

        // ✅ :PARAM を抽出してバインド
        var matches = Regex.Matches(sql, @"\:([A-Za-z0-9_]+)");
        foreach (Match m in matches)
        {
            string name = m.Groups[1].Value;
            if (!paramDict.ContainsKey(name))
                throw new Exception($"Parameter '{name}' が paramDict に存在しません");

            cmd.Parameters.Add(
                new OracleParameter(name, paramDict[name] ?? DBNull.Value)
            );
        }

        // ✅ 実行
        using var adapter = new OracleDataAdapter(cmd);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }
}
