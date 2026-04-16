using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Data;

public interface IDataSource
{
    Task<DataTable> ExecuteAsync(
        string connectionString,
        string sql,
        Dictionary<string, object?> parameters
    );
}
