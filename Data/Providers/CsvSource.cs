using AcrossReportDesigner.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Data.Providers;

/// <summary>
/// CSV ファイルを DB と同列の DataSource として扱う Provider
/// </summary>
public sealed class CsvSource : IDataSource
{
    /// <summary>
    /// CSV は connectionString に「ファイルパス」を渡す
    /// SQL / Parameters は未使用
    /// </summary>
    public async Task<DataTable> ExecuteAsync(
        string connectionString,
        string sql,
        Dictionary<string, object?> parameters)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("CSV ファイルパスが指定されていません。");

        if (!File.Exists(connectionString))
            throw new FileNotFoundException("CSV ファイルが見つかりません。", connectionString);

        var table = new DataTable();

        using var reader = new StreamReader(connectionString, Encoding.UTF8);

        // --- ヘッダ行 ---
        var headerLine = await reader.ReadLineAsync();
        if (headerLine == null)
            return table;

        var headers = SplitCsvLine(headerLine);

        foreach (var header in headers)
        {
            table.Columns.Add(header.Trim());
        }

        // --- データ行 ---
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = SplitCsvLine(line);
            table.Rows.Add(values);
        }

        return table;
    }

    /// <summary>
    /// シンプルな CSV 分割（ダブルクォート対応）
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());
        return values.ToArray();
    }
}
