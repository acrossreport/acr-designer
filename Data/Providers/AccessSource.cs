using AcrossReportDesigner.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Data.Providers;

public sealed class AccessSource : IDataSource
{
    public async Task<DataTable> ExecuteAsync(
        string connectionString,
        string sql,
        Dictionary<string, object?> parameters)
    {
        var table = new DataTable();

        using var conn = new OleDbConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();

        // ✅ OLE DB は名前付きパラメータ非対応
        //    :NAME / @NAME を ? に置換し、出現順にパラメータを追加する
        var orderedValues = new List<object>();
        string replacedSql = Regex.Replace(sql, @"[:@][A-Za-z0-9_]+", m =>
        {
            string key = m.Value.TrimStart(':', '@');
            if (parameters.TryGetValue(key, out var val))
                orderedValues.Add(val ?? DBNull.Value);
            else
                orderedValues.Add(DBNull.Value);
            return "?";
        });

        cmd.CommandText = replacedSql;

        foreach (var value in orderedValues)
        {
            cmd.Parameters.AddWithValue("?", value);
        }

        using var reader = await cmd.ExecuteReaderAsync();
        table.Load(reader);

        return table;
    }
}
