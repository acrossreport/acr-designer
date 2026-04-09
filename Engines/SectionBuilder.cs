using AcrossReportDesigner.Models;
using System.Collections.Generic;
using System.Linq;

namespace AcrossReportDesigner.Engines
{
    public static class SectionBuilder
    {
        public static List<SectionNode> Build(List<TemplateNode> nodes)
        {
            var result = new List<SectionNode>();

            if (nodes == null || nodes.Count == 0)
                return result;

            string? current = null;
            SectionNode? section = null;

            foreach (var node in nodes)   // ← 並び替えない
            {
                if (current != node.SectionName)
                {
                    section = new SectionNode
                    {
                        Name = node.SectionName,
                        Kind = node.SectionKind,
                        Height = node.SectionHeight,
                        GroupLevel = node.GroupLevel,
                        GroupKeyField = node.GroupKeyField,
                        GroupNewPage = node.GroupNewPage,
                        RepeatOnNewPage = node.RepeatOnNewPage,
                        KeepTogether = node.KeepTogether,
                        KeepWithNext = node.KeepWithNext,
                        Controls = new List<TemplateNode>()
                    };

                    result.Add(section);
                    current = node.SectionName;
                }

                section!.Controls.Add(node);
            }

            return result;
        }
    }
}

