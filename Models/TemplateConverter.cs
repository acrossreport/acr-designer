using AcrossReportDesigner.Models;
using System.Collections.Generic;
using System.Linq;
using AcrossReportDesigner.Engines;

namespace AcrossReportDesigner.Models
{
    public static class TemplateConverter
    {
        public static List<SectionDef> Convert(List<TemplateNode> template)
        {
            var result =
                new List<SectionDef>();

            if (template == null ||
                template.Count == 0)
                return result;

            // ★ TemplateNode → SectionInfo に変換
            var sections =
                SectionBuilder.Build(template);

            foreach (var sec in sections)
            {
                var section =
                    new SectionDef
                    {
                        Name = sec.Name,
                        Kind = sec.Kind,
                        GroupLevel = sec.GroupLevel,
                        GroupKeyField = sec.GroupKeyField,
                        GroupNewPage = sec.GroupNewPage,
                        RepeatOnNewPage = sec.RepeatOnNewPage,
                        KeepTogether = sec.KeepTogether,
                        KeepWithNext = sec.KeepWithNext,
                        Height = sec.Height,
                        ZIndex = 0
                    };

                foreach (var t in sec.Controls)
                {
                    section.Elements.Add(
                        new ElementDef
                        {
                            Type = t.Type.ToString(),
                            Name = t.Name,
                            Left = t.Left,
                            Top = t.Top,
                            Width = t.Width,
                            Height = t.Height,
                            Text = t.Text,
                            DataField = t.DataField,
                            ForeColor = t.ForeColor,
                            BackColor = t.BackColor,
                            BackStyle = t.BackStyle,
                            LineColor = t.LineColor,
                            LineWidth = t.LineWidth,
                            FontName = t.FontName,
                            FontSize = t.FontSize,
                            FontMode = t.FontMode,
                            ZIndex = t.ZIndex
                        });
                }

                result.Add(section);
            }

            return result;
        }
    }
}
