using System;

namespace AcrossReportDesigner.Data;

public sealed class AcrDbException : Exception
{
    public AcrDbType DbType { get; }
    public string Sql { get; }

    public AcrDbException(AcrDbType dbType, string sql, Exception inner)
        : base($"[ACR:{dbType}] {inner.Message}", inner)
    {
        DbType = dbType;
        Sql    = sql;
    }
}
