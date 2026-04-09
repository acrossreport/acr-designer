using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

namespace AcrossReportDesigner.Services
{
    /// <summary>
    /// テンプレートJSON + データJSON → インラインCSS HTML 変換
    /// セクション構造：
    ///   Section[0] = レポートヘッダー（1回）
    ///   Section[1] = グループヘッダー（DENPYONO変わり目）
    ///   Section[2] = 伝票ヘッダー（DENPYONO変わり目）
    ///   Section[3] = 明細行（各行繰り返し）
    ///   Section[4] = 合計行（DENPYONO変わり目）
    ///   Section[5] = 空
    ///   Section[6] = レポートフッター（1回）
    /// </summary>
    public static class TemplateHtmlBuilder
    {
        private const double TwipsToPx = 96.0 / 1440.0;

        public static string Build(string templateJson, string? dataJson = null)
        {
            // ========================
            // データ取得
            // ========================
            var allRows = new List<Dictionary<string, string?>>();
            if (dataJson != null)
            {
                try
                {
                    var dataRoot = JsonNode.Parse(dataJson);
                    JsonArray? dataArray =
                        dataRoot?["Parameters"]?["Data"]?.AsArray()
                        ?? (dataRoot is JsonArray arr ? arr : null);

                    if (dataArray != null)
                        foreach (var item in dataArray)
                        {
                            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                            var obj = item?.AsObject();
                            if (obj != null)
                                foreach (var kv in obj)
                                    row[kv.Key] = kv.Value?.ToString();
                            allRows.Add(row);
                        }
                }
                catch { }
            }

            // データなしの場合はダミー1行
            if (allRows.Count == 0)
                allRows.Add(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));

            // ========================
            // テンプレート解析
            // ========================
            var root = JsonNode.Parse(templateJson)!;
            var report = root["ACR"]!["Report"]!;
            var page = report["PageSettings"]!;

            double paperW = ToDouble(page["PaperWidth"])  * TwipsToPx;
            double paperH = ToDouble(page["PaperHeight"]) * TwipsToPx;
            int orientation = (int)ToDouble(page["Orientation"]);
            double pageW = orientation == 2 ? paperH : paperW;
            double pageH = orientation == 2 ? paperW : paperH;

            double marginL  = ToDouble(page["LeftMargin"])  * TwipsToPx;
            double marginT  = ToDouble(page["TopMargin"])   * TwipsToPx;
            double marginR  = ToDouble(page["RightMargin"]) * TwipsToPx;
            double contentW = pageW - marginL - marginR;

            var sections = report["Sections"]!["Section"]!.AsArray();
            // セクションを番号で取得
            var sec = new JsonNode?[Math.Max(7, sections.Count)];
            for (int i = 0; i < sections.Count; i++)
                sec[i] = sections[i];

            // ========================
            // DENPYONOでグループ化
            // ========================
            var groups = allRows
                .GroupBy(r => r.TryGetValue("DENPYONO", out var v) ? v ?? "" : "")
                .ToList();

            // ========================
            // HTML生成
            // ========================
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='ja'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1'>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body style=\"margin:0; background:#888;\">");

            // ページ外枠（縦に積み重なる）
            sb.AppendLine($"<div style=\"width:{pageW:F1}px; margin:0 auto;\">");

            // --- レポートヘッダー（Section[0]）: 1回 ---
            var firstRow = allRows[0];
            if (sec[0] != null)
                RenderSection(sb, sec[0]!, contentW, marginL, marginT, firstRow, pageW);

            // --- グループ繰り返し ---
            foreach (var grp in groups)
            {
                var grpRows = grp.ToList();
                var headerRow = grpRows[0]; // グループの先頭行

                // Section[1] グループヘッダー
                if (sec[1] != null)
                    RenderSection(sb, sec[1]!, contentW, marginL, 0, headerRow, pageW);

                // Section[2] 伝票ヘッダー
                if (sec[2] != null)
                    RenderSection(sb, sec[2]!, contentW, marginL, 0, headerRow, pageW);

                // Section[3] 明細行（各行繰り返し）
                if (sec[3] != null)
                    foreach (var row in grpRows)
                        RenderSection(sb, sec[3]!, contentW, marginL, 0, row, pageW);

                // Section[4] 合計行（先頭行のAMOUNTCOST/AMOUNTSELLを使用）
                if (sec[4] != null)
                    RenderSection(sb, sec[4]!, contentW, marginL, 0, headerRow, pageW);
            }

            // --- レポートフッター（Section[6]）: 1回 ---
            if (sec[6] != null)
                RenderSection(sb, sec[6]!, contentW, marginL, 0, firstRow, pageW);

            sb.AppendLine("</div>"); // 外枠
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        // ========================
        // セクション描画
        // ========================
        private static void RenderSection(StringBuilder sb, JsonNode section,
            double contentW, double marginL, double marginT,
            Dictionary<string, string?> row, double pageW)
        {
            double sectionH = ToDouble(section["Height"]) * TwipsToPx;
            if (sectionH <= 0) return;

            string sectionBg = "";
            var bgColor = section["BackColor"];
            if (bgColor != null)
            {
                int bgInt = (int)ToDouble(bgColor);
                if (bgInt != 16777215)
                    sectionBg = $" background:{IntToRgb(bgInt)};";
            }

            // マージンオフセット付き
            string marginCss = marginT > 0
                ? $" margin-left:{marginL:F1}px; margin-top:{marginT:F1}px;"
                : $" margin-left:{marginL:F1}px;";

            sb.AppendLine(
                $"<div style=\"position:relative; width:{contentW:F1}px; height:{sectionH:F1}px;{sectionBg}{marginCss}\">");

            var controls = section["Control"]?.AsArray();
            if (controls != null)
                foreach (var ctrl in controls)
                    if (ctrl != null)
                        RenderControl(sb, ctrl,
                            ctrl["Type"]?.GetValue<string>() ?? "",
                            contentW, row);

            sb.AppendLine("</div>");
        }

        // ========================
        // コントロール振り分け
        // ========================
        private static void RenderControl(StringBuilder sb, JsonNode ctrl, string type,
            double contentW, Dictionary<string, string?> row)
        {
            double left   = ToDouble(ctrl["Left"])   * TwipsToPx;
            double top    = ToDouble(ctrl["Top"])    * TwipsToPx;
            double width  = ToDouble(ctrl["Width"])  * TwipsToPx;
            double height = ToDouble(ctrl["Height"]) * TwipsToPx;
            if (width <= 0 || height <= 0) return;

            switch (type)
            {
                case "ACR.Label":
                case "ACR.Field":
                    RenderText(sb, ctrl, left, top, width, height, row);
                    break;
                case "ACR.Shape":
                    RenderShape(sb, ctrl, left, top, width, height);
                    break;
                case "ACR.Line":
                    RenderLine(sb, ctrl, contentW);
                    break;
            }
        }

        // ========================
        // テキスト描画
        // ========================
        private static void RenderText(StringBuilder sb, JsonNode ctrl,
            double left, double top, double width, double height,
            Dictionary<string, string?> row)
        {
            string text = "";
            string? dataField = ctrl["DataField"]?.GetValue<string>();

            if (!string.IsNullOrEmpty(dataField))
            {
                if (row.TryGetValue(dataField, out var val))
                    text = val ?? "";
                else
                    text = "";
            }
            else
            {
                var textNode = ctrl["Text"];
                if (textNode != null)
                    text = textNode.ToString();
            }

            string style  = ctrl["Style"]?.GetValue<string>() ?? "";
            string bgCss  = "";
            var backStyle = ctrl["BackStyle"];
            var backColor = ctrl["BackColor"];
            if (backStyle != null && (int)ToDouble(backStyle) == 1 && backColor != null)
                bgCss = $" background:{IntToRgb((int)ToDouble(backColor))};";

            sb.AppendLine(
                $"<div style=\"position:absolute; left:{left:F1}px; top:{top:F1}px; " +
                $"width:{width:F1}px; height:{height:F1}px; " +
                $"overflow:hidden; box-sizing:border-box;{bgCss} {style}\">" +
                $"{HtmlEncode(text)}</div>");
        }

        // ========================
        // Shape描画
        // ========================
        private static void RenderShape(StringBuilder sb, JsonNode ctrl,
            double left, double top, double width, double height)
        {
            string bgCss = "transparent";
            var backStyle = ctrl["BackStyle"];
            var backColor = ctrl["BackColor"];
            if (backStyle != null && (int)ToDouble(backStyle) == 1 && backColor != null)
                bgCss = IntToRgb((int)ToDouble(backColor));

            string borderCss = "";
            var lineColor  = ctrl["LineColor"];
            var lineWeight = ctrl["LineWeight"];
            if (lineColor != null)
            {
                double lw = lineWeight != null ? ToDouble(lineWeight) * TwipsToPx : 1.0;
                borderCss = $" border:{lw:F1}px solid {IntToRgb((int)ToDouble(lineColor))};";
            }

            sb.AppendLine(
                $"<div style=\"position:absolute; left:{left:F1}px; top:{top:F1}px; " +
                $"width:{width:F1}px; height:{height:F1}px; " +
                $"background:{bgCss};{borderCss} box-sizing:border-box;\"></div>");
        }

        // ========================
        // Line描画
        // ========================
        private static void RenderLine(StringBuilder sb, JsonNode ctrl, double contentW)
        {
            var lineColor  = ctrl["LineColor"];
            string color   = lineColor != null ? IntToRgb((int)ToDouble(lineColor)) : "#000";
            var lineWeight = ctrl["LineWeight"];
            double lw      = lineWeight != null ? Math.Max(1, ToDouble(lineWeight) * TwipsToPx) : 1.0;

            double left  = ToDouble(ctrl["Left"])  * TwipsToPx;
            double top   = ToDouble(ctrl["Top"])   * TwipsToPx;
            double width = ToDouble(ctrl["Width"]) * TwipsToPx;
            if (width <= 0) width = contentW;

            sb.AppendLine(
                $"<div style=\"position:absolute; left:{left:F1}px; top:{top:F1}px; " +
                $"width:{width:F1}px; height:{lw:F1}px; background:{color};\"></div>");
        }

        // ========================
        // ユーティリティ
        // ========================
        private static string IntToRgb(int color)
        {
            int r = color & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = (color >> 16) & 0xFF;
            return $"rgb({r},{g},{b})";
        }

        private static double ToDouble(JsonNode? node)
        {
            if (node == null) return 0;
            try { return node.GetValue<double>(); } catch { }
            try { return node.GetValue<int>();    } catch { }
            try { return double.Parse(node.ToString()); } catch { }
            return 0;
        }

        private static string HtmlEncode(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
