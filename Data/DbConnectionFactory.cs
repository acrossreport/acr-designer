using AcrossReportDesigner.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data.Common;
using System.Threading.Tasks;
#if WINDOWS
using System.Data.OleDb;
#endif

namespace AcrossReportDesigner.Data;

public static class DbConnectionFactory
{
    /// <summary>
    /// DB種別に応じた DbConnection を生成する
    /// </summary>
    public static DbConnection Create(AcrDbType dbType, string connectionString)
    {
        return dbType switch
        {
            AcrDbType.Oracle     => new OracleConnection(connectionString),
            AcrDbType.SqlServer  => new SqlConnection(connectionString),
            AcrDbType.PostgreSql => new NpgsqlConnection(connectionString),
            AcrDbType.MySql      => new MySqlConnection(connectionString),
            AcrDbType.Sqlite     => new SqliteConnection(connectionString),
#if WINDOWS
            AcrDbType.Access     => new OleDbConnection(connectionString),
#endif
            _ => throw new NotSupportedException($"未対応のDB種別: {dbType}")
        };
    }

    /// <summary>
    /// 接続テスト用
    /// Oracle は OpenAsync が不完全なため Task.Run で同期実行
    /// その他は OpenAsync を使用
    /// </summary>
    public static async Task TestConnectionAsync(AcrDbType dbType, string connectionString)
    {
        using var conn = Create(dbType, connectionString);

        if (dbType == AcrDbType.Oracle)
        {
            await Task.Run(() =>
            {
                conn.Open();
                conn.Close();
            });
        }
        else
        {
            await conn.OpenAsync();
            conn.Close();
        }
    }
}
