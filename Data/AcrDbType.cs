namespace AcrossReportDesigner.Data;

// ✅ XAMLコンボボックスの順番に合わせる
public enum AcrDbType
{
    Oracle,      // 0
    SqlServer,   // 1
    PostgreSql,  // 2
    MySql,       // 3
    Sqlite,      // 4
    Csv,         // 5
#if WINDOWS
    Access       // 6
#endif
}
