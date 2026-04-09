using System.Text;
using System.Text.Json;

namespace AcrossReportDesigner.Rendering
{
    public static class ReportHtmlGenerator
    {
        // ★ テンプレート＋データを受け取る
        public static string Render(string templateJson, string dataJson)
        {
            // ① JSON をパース
            using var templateDoc = JsonDocument.Parse(templateJson);
            using var dataDoc = JsonDocument.Parse(dataJson);

            // ②（仮）今はデバッグ表示
            //    → 次の段階で RenderNode 化する
            return BuildDebugHtml(templateDoc, dataDoc);
        }

        // 既存のテストHTML生成を内部に移す
        private static string BuildDebugHtml(
            JsonDocument templateDoc,
            JsonDocument dataDoc)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"ja\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'MS Gothic'; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine("<h2>Template JSON</h2>");
            sb.AppendLine("<pre>");
            sb.AppendLine(
                JsonSerializer.Serialize(
                    templateDoc.RootElement,
                    new JsonSerializerOptions { WriteIndented = true }));
            sb.AppendLine("</pre>");

            sb.AppendLine("<h2>Data JSON</h2>");
            sb.AppendLine("<pre>");
            sb.AppendLine(
                JsonSerializer.Serialize(
                    dataDoc.RootElement,
                    new JsonSerializerOptions { WriteIndented = true }));
            sb.AppendLine("</pre>");

            sb.AppendLine("</body></html>");

            return sb.ToString();
        }
    }
}
