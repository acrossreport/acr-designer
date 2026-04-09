using AcrossReportDesigner.Models;
using System.Collections.Generic;

namespace AcrossReportDesigner.Services;

public sealed class ReportFileRoot
{
    public ReportMeta Meta { get; set; } = new();
    public List<ReportItem> Items { get; set; } = new();
}
