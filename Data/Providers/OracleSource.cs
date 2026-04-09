using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public sealed class OracleSource : IDataSource
{
    public async Task<DataTable> ExecuteAsync(
        string connStr,
        string sql,
        Dictionary<string, object?> paramDict)
    {
        using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // ✅ Oracleは必須
        cmd.BindByName = true;

        // ✅ SQL文の中に出てくる :PARAM を順番に抽出
        var matches = Regex.Matches(sql, @"\:([A-Za-z0-9_]+)");

        foreach (Match m in matches)
        {
            string name = m.Groups[1].Value;

            if (!paramDict.ContainsKey(name))
            {
                throw new Exception($"Parameter '{name}' が paramDict に存在しません");
            }

            cmd.Parameters.Add(
                new OracleParameter(name, paramDict[name] ?? DBNull.Value)
            );
        }

        // ✅ 実行
        using var adapter = new OracleDataAdapter(cmd);
        var table = new DataTable();
        int cnt = table.Rows.Count;
        adapter.Fill(table);

        return table;
    }
}
