using System.Text.Json.Nodes;

namespace AcrossReportDesigner.Models;

public sealed class DesignControl
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public double LeftMm { get; set; }
    public double TopMm { get; set; }
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public string Text { get; set; } = "";
    public string DataField { get; set; } = "";
    // ★追加
    public bool CanGrow { get; set; } = false;
    public bool CanShrink { get; set; } = false;
    public bool MultiLine { get; set; } = false;
    public bool Visible { get; set; } = true;
    public string Alignment { get; set; } = "Left";  // Left/Center/Right
    // ★フォント（テンプレから読む）
    public string FontName { get; set; } = "MS Gothic";
    public double FontSize { get; set; } = 10;
    public double X1Mm { get; set; }
    public double Y1Mm { get; set; }
    public double X2Mm { get; set; }
    public double Y2Mm { get; set; }
    public double LineWidth { get; set; } = 0.4;
    public JsonNode? SourceNode { get; set; }
    // ✅フォント情報
    public string FontFamily { get; set; } = "MS Gothic";
    public double FontSizePt { get; set; } = 10.5;
    public bool Bold { get; set; } = false;
    public string TextAlign { get; set; } = "left";
    // ✅テンプレ互換Style文字列
    public string Style { get; set; } = "";
    // ✅線種（ActiveReports互換）
    public int LineStyle { get; set; } = 0;
    public TableDefinition? Table { get; set; }
    // ======================================================
    // ✅✅ Shape / 背景色対応（ここが今回の追加）
    // ======================================================
    public int ForeColor { get; set; } = unchecked((int)0xFF000000);
    public int BackColor { get; set; } = unchecked((int)0x00000000);
    public int BackStyle { get; set; } = 0;
    public int LineColor { get; set; } = unchecked((int)0xFF000000);
    public double RoundingRadius { get; set; } = 0;
    public int ZIndex { get; set; } = 0;

    // ======================================================
    // ✅ ファクトリ（ドラッグドロップ生成用）
    // ======================================================
    public static DesignControl CreateText(double leftMm, double topMm)
    {
        return new DesignControl
        {
            Type = "Text",
            Name = "Text",
            LeftMm = leftMm,
            TopMm = topMm,
            WidthMm = 30,
            HeightMm = 8,
            Text = "Text"
        };
    }
    public static DesignControl CreateLabel(double leftMm, double topMm)
    {
        return new DesignControl
        {
            Type = "Label",
            Name = "Label",
            LeftMm = leftMm,
            TopMm = topMm,
            WidthMm = 25,
            HeightMm = 8,
            Text = "Label"
        };
    }
    public static DesignControl CreateLine(double leftMm, double topMm)
    {
        return new DesignControl
        {
            Type = "Line",
            Name = "Line",
            LeftMm = leftMm,
            TopMm = topMm,
            X1Mm = leftMm,
            Y1Mm = topMm,
            X2Mm = leftMm + 30,
            Y2Mm = topMm,
            LineWidth = 0.4
        };
    }

    public static DesignControl CreateShape(double leftMm, double topMm)
    {
        return new DesignControl
        {
            Type = "Shape",
            Name = "Shape",
            LeftMm = leftMm,
            TopMm = topMm,
            WidthMm = 20,
            HeightMm = 20,
            BackStyle = 1,
            LineWidth = 0.4
        };
    }
}
