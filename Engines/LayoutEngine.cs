using AcrossReportDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AcrossReportDesigner.Engines
{
    public static class LayoutEngine
    {
        public static List<RenderNode> Build(
            List<TemplateNode> templateNodes,
            List<Dictionary<string, string>> rows,
            double pageWidthTwips,
            double pageHeightTwips,
            double leftMarginTwips,
            double topMarginTwips,
            double rightMarginTwips,
            double bottomMarginTwips)
        {
            var result = new List<RenderNode>();

            // セクション構築
            List<SectionNode> sections = SectionBuilder.Build(templateNodes);
            // ここにログを追加
            foreach (var s in sections)
            {
                Console.WriteLine(
                    $"SECTION: {s.Kind}  Height={s.Height}  Controls={s.Controls.Count}");
            }

            int pageIndex = 0;
            double yOffset = topMarginTwips;
            double usableBottom = pageHeightTwips - bottomMarginTwips;

            // --- Group フィールド取得（上位→下位の順で並んでいる前提）
            var groupFields = sections
                .Where(s => s.Kind == SectionKind.GroupHeader)
                .OrderBy(s => s.GroupLevel)
                .Select(s => s.GroupKeyField)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();

            var groupCtx = new GroupLayoutContext(groupFields);
            groupCtx.Reset();

            // --- ページヘッダ（あれば最初に出す）
            foreach (var header in sections.Where(s => s.Kind == SectionKind.PageHeader))
            {
                RenderSection(result, header, null,
                    ref pageIndex, ref yOffset,
                    usableBottom, leftMarginTwips, topMarginTwips);
            }

            // =========================
            // rows が外側（重要）
            // =========================
            foreach (var row in rows)
            {
                int changedLevel = groupCtx.DetectFirstChangedLevel(row);

                // 変化があった場合
                if (changedLevel >= 0)
                {
                    // --- 下位から閉じる
                    for (int level = groupCtx.LevelCount - 1; level >= changedLevel; level--)
                    {
                        var footer = sections
                            .FirstOrDefault(s =>
                                s.Kind == SectionKind.GroupFooter &&
                                s.GroupLevel == level);

                        if (footer != null)
                        {
                            RenderSection(result, footer, row,
                                ref pageIndex, ref yOffset,
                                usableBottom, leftMarginTwips, topMarginTwips);
                        }
                    }

                    // --- 上位から開く
                    for (int level = changedLevel; level < groupCtx.LevelCount; level++)
                    {
                        var header = sections
                            .FirstOrDefault(s =>
                                s.Kind == SectionKind.GroupHeader &&
                                s.GroupLevel == level);

                        if (header != null)
                        {
                            RenderSection(result, header, row,
                                ref pageIndex, ref yOffset,
                                usableBottom, leftMarginTwips, topMarginTwips);
                        }
                    }
                }

                // --- Detail
                var detail = sections
                    .FirstOrDefault(s => s.Kind == SectionKind.Detail);

                if (detail != null)
                {
                    RenderSection(result, detail, row,
                        ref pageIndex, ref yOffset,
                        usableBottom, leftMarginTwips, topMarginTwips);
                }
            }

            // --- 最後に全部閉じる
            for (int level = groupCtx.LevelCount - 1; level >= 0; level--)
            {
                var footer = sections
                    .FirstOrDefault(s =>
                        s.Kind == SectionKind.GroupFooter &&
                        s.GroupLevel == level);

                if (footer != null)
                {
                    RenderSection(result, footer, null,
                        ref pageIndex, ref yOffset,
                        usableBottom, leftMarginTwips, topMarginTwips);
                }
            }

            // --- ページフッタ
            foreach (var footer in sections.Where(s => s.Kind == SectionKind.PageFooter))
            {
                RenderSection(result, footer, null,
                    ref pageIndex, ref yOffset,
                    usableBottom, leftMarginTwips, topMarginTwips);
            }

            return result;
        }

        private static void RenderSection(
            List<RenderNode> result,
            SectionNode section,
            Dictionary<string, string>? row,
            ref int pageIndex,
            ref double yOffset,
            double usableBottom,
            double leftMarginTwips,
            double topMarginTwips)
        {
            if (yOffset + section.Height > usableBottom)
            {
                pageIndex++;
                yOffset = topMarginTwips;
            }

            foreach (TemplateNode node in section.Controls)
            {
                RenderNode rn = CreateRenderNode(node, row);

                rn.PageIndex = pageIndex;
                rn.Left = node.Left + leftMarginTwips;
                rn.Top = yOffset + node.Top;

                result.Add(rn);
            }

            yOffset += section.Height;
        }

        private static RenderNode CreateRenderNode(
            TemplateNode node,
            Dictionary<string, string>? row)
        {
            var rn = new RenderNode
            {
                Left = node.Left,
                Top = node.Top,
                Width = node.Width,
                Height = node.Height,
                FontName = node.FontName,
                FontSize = node.FontSize,
                ForeColor = node.ForeColor,
                BackColor = node.BackColor,
                BackStyle = node.BackStyle,
                LineColor = node.LineColor,
                LineWidth = node.LineWidth,
                ZIndex = node.ZIndex
            };

            if (!string.IsNullOrEmpty(node.DataField) &&
                row != null &&
                row.TryGetValue(node.DataField, out var value))
            {
                rn.Text = value;
            }
            else
            {
                rn.Text = node.Text ?? string.Empty;
            }

            return rn;
        }
    }
}
