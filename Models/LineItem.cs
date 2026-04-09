using System;
using System.Text.Json.Serialization;

namespace AcrossReportDesigner.Models;

public class LineItem : ReportItem
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double Thickness { get; set; }
    public bool IsVertical => Math.Abs(X1 - X2) < double.Epsilon;
}
