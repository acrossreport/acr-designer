using AcrossReportDesigner.Data.Providers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Data;

public sealed class DbProvider
{
    private readonly AcrDbType _dbType;
    private readonly IDataSource _source;

    public DbProvider(AcrDbType dbType)
    {
        _dbType = dbType;
        _source = dbType switch
        {
            AcrDbType.Oracle     => new OracleSource(),
            AcrDbType.SqlServer  => new SqlServerSource(),
            AcrDbType.PostgreSql => new PostgreSqlSource(),
            AcrDbType.MySql      => new MySqlSource(),
            AcrDbType.Sqlite     => new SQLiteSource(),
            AcrDbType.Access     => new AccessSource(),
            _ => throw new NotSupportedException($"未対応のDB種別: {dbType}")
        };
    }

    public async Task<DataTable> ExecuteAsync(
        string connectionString,
        string sql,
        Dictionary<string, object?>? parameters = null)
    {
        parameters ??= new Dictionary<string, object?>();
        try
        {
            return await _source.ExecuteAsync(connectionString, sql, parameters);
        }
        catch (AcrDbException)
        {
            throw; // 二重ラップ防止
        }
        catch (Exception ex)
        {
            throw new AcrDbException(_dbType, sql, ex);
        }
    }
}
