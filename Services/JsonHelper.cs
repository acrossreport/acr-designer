using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;

namespace AcrossReportDesigner.Services;

public static class JsonHelper
{
    public static string DataTableToJson(DataTable table)
    {
        var rows = new List<Dictionary<string, object?>>();

        foreach (DataRow row in table.Rows)
        {
            var dict = new Dictionary<string, object?>();

            foreach (DataColumn col in table.Columns)
            {
                var value = row[col];
                dict[col.ColumnName] = value == DBNull.Value ? null : value;
            }

            rows.Add(dict);
        }

        return JsonSerializer.Serialize(
            rows,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }
}
