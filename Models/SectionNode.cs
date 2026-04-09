using System.Collections.Generic;

namespace AcrossReportDesigner.Models;

public sealed class SectionNode
{
    public string Name { get; set; } = "";

    public SectionKind Kind { get; set; }

    public double Height { get; set; }

    public int GroupLevel { get; set; }

    public string GroupKeyField { get; set; } = "";

    public bool GroupNewPage { get; set; }

    public bool RepeatOnNewPage { get; set; }

    public bool KeepTogether { get; set; }

    public bool KeepWithNext { get; set; }

    public PageBreakMode PageBreak { get; set; }

    public List<TemplateNode> Controls { get; set; } = new();
}
