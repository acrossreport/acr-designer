using AcrossReportDesigner.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace AcrossReportDesigner.Engines
{
    public static class TemplateZIndexBuilder
    {
        public static List<TemplateNode> Build(JsonElement root)
        {
            var nodes = new List<TemplateNode>();
            int order = 0;

            string currentSectionName = "";
            SectionKind currentSectionKind = SectionKind.Unknown;
            double currentSectionHeight = 0;

            int currentGroupLevel = 0;
            string currentGroupKeyField = "";
            bool currentGroupNewPage = false;

            Traverse(
                root,
                nodes,
                ref order,
                ref currentSectionName,
                ref currentSectionKind,
                ref currentSectionHeight,
                ref currentGroupLevel,
                ref currentGroupKeyField,
                ref currentGroupNewPage
            );

            FixSectionHeight(nodes, SectionKind.Detail, 300);
            FixSectionHeight(nodes, SectionKind.GroupHeader, 300);
            FixSectionHeight(nodes, SectionKind.GroupFooter, 300);

            return nodes;
        }

        private static void Traverse(
            JsonElement el,
            List<TemplateNode> nodes,
            ref int order,
            ref string currentSectionName,
            ref SectionKind currentSectionKind,
            ref double currentSectionHeight,
            ref int currentGroupLevel,
            ref string currentGroupKeyField,
            ref bool currentGroupNewPage)
        {
            if (el.ValueKind == JsonValueKind.Object)
            {
                if (IsSectionObject(
                    el,
                    out var secType,
                    out var secName,
                    out var secHeight,
                    out var secDataField,
                    out var secNewPage))
                {
                    currentSectionName = secName;
                    currentSectionKind = ToSectionKindByType(secType);
                    currentSectionHeight = secHeight;

                    if (currentSectionKind == SectionKind.GroupHeader ||
                        currentSectionKind == SectionKind.GroupFooter)
                        currentGroupLevel = ParseGroupLevel(secName);
                    else
                        currentGroupLevel = 0;

                    currentGroupKeyField = secDataField;
                    currentGroupNewPage = secNewPage != 0;
                }

                if (el.TryGetProperty("Type", out var typeProp))
                {
                    string type = typeProp.GetString() ?? "";

                    if (IsRenderableControlType(type))
                    {
                        nodes.Add(CreateNode(
                            el,
                            type,
                            order++,
                            currentSectionName,
                            currentSectionKind,
                            currentSectionHeight,
                            currentGroupLevel,
                            currentGroupKeyField,
                            currentGroupNewPage));
                    }
                }

                foreach (var p in el.EnumerateObject())
                {
                    Traverse(
                        p.Value,
                        nodes,
                        ref order,
                        ref currentSectionName,
                        ref currentSectionKind,
                        ref currentSectionHeight,
                        ref currentGroupLevel,
                        ref currentGroupKeyField,
                        ref currentGroupNewPage);
                }
            }
            else if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in el.EnumerateArray())
                {
                    Traverse(
                        c,
                        nodes,
                        ref order,
                        ref currentSectionName,
                        ref currentSectionKind,
                        ref currentSectionHeight,
                        ref currentGroupLevel,
                        ref currentGroupKeyField,
                        ref currentGroupNewPage);
                }
            }
        }
        private static TemplateNode CreateNode(
            JsonElement el,
            string type,
            int order,
            string sectionName,
            SectionKind sectionKind,
            double sectionHeight,
            int groupLevel,
            string groupKeyField,
            bool groupNewPage)
        {
            var node = new TemplateNode();

            if (Enum.TryParse<TemplateNodeType>(type, true, out var parsedType))
                node.Type = parsedType;
            else
                node.Type = TemplateNodeType.Control;

            node.Name = GetString(el, "Name");

            node.Left = GetDouble(el, "Left");
            node.Top = GetDouble(el, "Top");
            node.Width = GetDouble(el, "Width");
            node.Height = GetDouble(el, "Height");

            node.Text = GetAnyAsString(el, "Text");
            node.DataField = GetString(el, "DataField");

            node.ForeColor = GetInt(el, "ForeColor");
            node.BackColor = GetInt(el, "BackColor");
            node.BackStyle = GetInt(el, "BackStyle");
            node.LineColor = GetInt(el, "LineColor");
            node.LineWidth = GetDouble(el, "LineWidth");

            node.FontName = GetString(el, "FontName", "MS UI Gothic");
            node.FontSize = GetDouble(el, "FontSize", 8.25);

            node.SectionName = sectionName;
            node.SectionKind = sectionKind;
            node.SectionHeight = sectionHeight;

            node.GroupLevel = groupLevel;
            node.GroupKeyField = groupKeyField;
            node.GroupNewPage = groupNewPage;

            node.RepeatOnNewPage =
                el.TryGetProperty("RepeatOnNewPage", out var rnp)
                && rnp.GetInt32() != 0;

            node.KeepTogether =
                el.TryGetProperty("KeepTogether", out var kt)
                && kt.GetInt32() != 0;

            node.KeepWithNext =
                el.TryGetProperty("KeepWithNext", out var kwn)
                && kwn.GetInt32() != 0;

            node.PageBreak =
                el.TryGetProperty("PageBreak", out var pb)
                ? (PageBreakMode)pb.GetInt32()
                : PageBreakMode.None;

            node.ZIndex = CalcZIndex(node, order);

            return node;
        }

        private static int CalcZIndex(TemplateNode n, int order)
        {
            int baseZ = n.ControlType switch
            {
                ControlType.Shape => 1000,
                ControlType.Line => 1200,
                ControlType.Label => 3000,
                ControlType.Field => 3200,
                ControlType.Text => 3500,
                _ => 4000
            };
            return baseZ + order;
        }

        private static bool IsSectionObject(
            JsonElement el,
            out string secType,
            out string secName,
            out double secHeight,
            out string secDataField,
            out int secNewPage)
        {
            secType = GetString(el, "Type");
            secName = GetString(el, "Name");
            secHeight = GetDouble(el, "Height");
            secDataField = GetString(el, "DataField");
            secNewPage = GetInt(el, "NewPage");

            if (string.IsNullOrEmpty(secType))
                return false;

            string t = secType.ToLower();

            return t.Contains("detail")
                || t.Contains("groupheader")
                || t.Contains("groupfooter")
                || t.Contains("pageheader")
                || t.Contains("pagefooter")
                || t.Contains("reportheader")
                || t.Contains("reportfooter");
        }
        private static SectionKind ToSectionKindByType(string secType)
        {
            string t = secType.ToLower();

            if (t.Contains("reportheader")) return SectionKind.ReportHeader;
            if (t.Contains("pageheader")) return SectionKind.PageHeader;
            if (t.Contains("groupheader")) return SectionKind.GroupHeader;
            if (t.Contains("detail")) return SectionKind.Detail;
            if (t.Contains("groupfooter")) return SectionKind.GroupFooter;
            if (t.Contains("pagefooter")) return SectionKind.PageFooter;
            if (t.Contains("reportfooter")) return SectionKind.ReportFooter;

            return SectionKind.Unknown;
        }

        private static int ParseGroupLevel(string name)
        {
            var m = System.Text.RegularExpressions.Regex.Match(name, @"(\d+)$");
            return m.Success ? int.Parse(m.Groups[1].Value) : 1;
        }

        private static bool IsRenderableControlType(string type)
        {
            return type.StartsWith("AR.")
                || type.StartsWith("ACR.");
        }

        private static void FixSectionHeight(
            List<TemplateNode> nodes,
            SectionKind kind,
            double fallback)
        {
            var groups = nodes
                .Where(n => n.SectionKind == kind)
                .GroupBy(n => n.SectionName)
                .ToList();

            foreach (var g in groups)
            {
                if (g.Any(n => n.SectionHeight > 0))
                    continue;

                double top = g.Min(n => n.Top);
                double bottom = g.Max(n => n.Top + n.Height);

                double h = bottom - top;

                if (h <= 0)
                    h = fallback;

                foreach (var n in g)
                    n.SectionHeight = h;
            }
        }

        private static string GetString(JsonElement el, string key, string def = "")
        {
            return el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() ?? def
                : def;
        }

        private static string GetAnyAsString(JsonElement el, string key, string def = "")
        {
            return el.TryGetProperty(key, out var v)
                ? v.ToString()
                : def;
        }

        private static double GetDouble(JsonElement el, string key, double def = 0)
        {
            if (!el.TryGetProperty(key, out var v)) return def;

            if (v.ValueKind == JsonValueKind.Number)
                return v.GetDouble();

            if (v.ValueKind == JsonValueKind.String &&
                double.TryParse(v.GetString(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var d))
                return d;

            return def;
        }

        private static int GetInt(JsonElement el, string key, int def = 0)
        {
            if (!el.TryGetProperty(key, out var v)) return def;

            if (v.ValueKind == JsonValueKind.Number)
                return v.GetInt32();

            if (v.ValueKind == JsonValueKind.String &&
                int.TryParse(v.GetString(), out var i))
                return i;

            return def;
        }
    }
}
