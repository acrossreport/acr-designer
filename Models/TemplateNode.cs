namespace AcrossReportDesigner.Models;

public sealed class TemplateNode
{
    public string Name { get; set; } = "";
    public TemplateNodeType Type { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public string Text { get; set; } = "";
    public string DataField { get; set; } = "";

    public string FontName { get; set; } = "";
    public double FontSize { get; set; } = 9;

    public int ForeColor { get; set; }
    public int BackColor { get; set; }
    public int BackStyle { get; set; }

    public int LineColor { get; set; }
    public double LineWidth { get; set; }

    public int ZIndex { get; set; }

    public string SectionName { get; set; } = "";
    public SectionKind SectionKind { get; set; }
    public double SectionHeight { get; set; }

    public int GroupLevel { get; set; }
    public string GroupKeyField { get; set; } = "";
    public bool GroupNewPage { get; set; }

    public bool RepeatOnNewPage { get; set; }
    public bool KeepTogether { get; set; }
    public bool KeepWithNext { get; set; }

    public PageBreakMode PageBreak { get; set; }
    public FontMode FontMode { get; set; }

    public ControlType ControlType { get; set; } = ControlType.None;

    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public override string ToString() => Name;
}

public enum TemplateNodeType
{
    Root,
    Section,
    Control
}
public enum ControlType
{
    None,
    Shape,
    Line,
    Label,
    Field,
    Text
}
