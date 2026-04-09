using AcrossReportDesigner.Models;
using System.Collections.Generic;

public sealed class SectionSnapshot
{
    public string Name { get; set; } = "";
    public double Top { get; set; }
    public double Height { get; set; }
    public bool Visible { get; set; } = true;
    public bool Repeat { get; set; }
    public string? BackColor { get; set; }
    public string? Border { get; set; }
    // ✅ Controls は後で追加すればよい
    public List<DesignControl> Controls { get; set; } = new();
}
