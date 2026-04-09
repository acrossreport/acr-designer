using AcrossReportDesigner.Data;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Data.Providers;

public sealed class SqlServerSource : IDataSource
{
    public async Task<DataTable> ExecuteAsync(
        string connectionString,
        string sql,
        Dictionary<string, object?> parameters)
    {
        var table = new DataTable();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // ✅ SQLServer Parameter追加
        foreach (var kv in parameters)
        {
            // SQLServerは @PARAM 形式
            string name = kv.Key.TrimStart(':', '@');
            object value = kv.Value ?? DBNull.Value;

            cmd.Parameters.AddWithValue("@" + name, value);
        }

        using var reader = await cmd.ExecuteReaderAsync();
        table.Load(reader);

        return table;
    }
}
