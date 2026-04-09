using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AcrossReportDesigner.Services;

public static class HtmlPreviewHelper
{
    public static void OpenInBrowser(string html)
    {
        // 出力先フォルダ
        string dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);

        // ファイル名
        string path = Path.Combine(
            dir,
            $"preview_{DateTime.Now:yyyyMMddHHmmss}.html");

        // UTF-8 で保存
        File.WriteAllText(path, html, Encoding.UTF8);

        // 既定ブラウザで開く
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
