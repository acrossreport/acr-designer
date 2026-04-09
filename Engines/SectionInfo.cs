using AcrossReportDesigner.Models;
using System.Collections.Generic;

namespace AcrossReportDesigner.Engines
{
    public sealed class SectionInfo
    {
        public string Name = "";

        public SectionKind Kind = SectionKind.Unknown;

        public double Height;

        public int GroupLevel;

        public string GroupKeyField = "";

        public bool GroupNewPage;

        public bool RepeatOnNewPage;

        public bool KeepTogether;

        public bool KeepWithNext;

        public List<TemplateNode> Nodes = new List<TemplateNode>();
    }
}
