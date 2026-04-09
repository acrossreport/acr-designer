using AcrossReportDesigner.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Data.Providers;

public sealed class SQLiteSource : IDataSource
{
    public async Task<DataTable> ExecuteAsync(
        string connectionString,
        string sql,
        Dictionary<string, object?> parameters)
    {
        var table = new DataTable();

        using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // ✅ パラメータ追加
        foreach (var kv in parameters)
        {
            string name = kv.Key.TrimStart(':', '@');
            object value = kv.Value ?? DBNull.Value;

            cmd.Parameters.AddWithValue(name, value);
        }

        using var reader = await cmd.ExecuteReaderAsync();
        table.Load(reader);

        return table;
    }
}
