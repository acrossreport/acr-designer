namespace AcrossReportDesigner.Services;

public sealed class ReportMeta
{
    public string DesignerName { get; set; } = "";
    public string DesignerVersion { get; set; } = "";
    public string EngineName { get; set; } = "";
    public string EngineVersion { get; set; } = "";
    public int EngineApiLevel { get; set; }
    public int FormatVersion { get; set; }
}
