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
    // ✅ Picture（ActiveReports: PictureBox互換）
    // ======================================================
    /// <summary>画像ファイルパス（デザイン時固定画像）</summary>
    public string ImagePath { get; set; } = "";
    /// <summary>DataFieldから取得するBase64画像（実行時）</summary>
    public string ImageDataField { get; set; } = "";
    /// <summary>
    /// AR互換 SizeMode:
    ///   0=Clip, 1=Stretch, 2=Zoom, 3=Center
    /// </summary>
    public int SizeMode { get; set; } = 2;  // デフォルトZoom

    // ======================================================
    // ✅ Barcode（ActiveReports: Barcode互換）
    // ======================================================
    /// <summary>
    /// AR互換 バーコード種別:
    ///   Code39, Code128, QRCode, JAN13, EAN8, NW7, ITF, PDF417, DataMatrix
    /// </summary>
    public string BarcodeType { get; set; } = "Code128";
    /// <summary>固定バーコード値</summary>
    public string BarcodeValue { get; set; } = "1234567890";
    /// <summary>DataFieldからバーコード値取得</summary>
    public string BarcodeDataField { get; set; } = "";
    /// <summary>バーコード下部にテキスト表示</summary>
    public bool BarcodeShowText { get; set; } = true;
    /// <summary>バーコード色（AR互換 OLE色）</summary>
    public int BarColor { get; set; } = 0x000000;
    /// <summary>QRコード誤り訂正レベル: L/M/Q/H</summary>
    public string QrErrorLevel { get; set; } = "M";

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

    // ======================================================
    // ✅ Picture ファクトリ（AR: PictureBox互換）
    // ======================================================
    public static DesignControl CreatePicture(double leftMm, double topMm)
    {
        return new DesignControl
        {
            Type = "Picture",
            Name = "Picture",
            LeftMm = leftMm,
            TopMm = topMm,
            WidthMm = 30,
            HeightMm = 20,
            SizeMode = 2,
            ImagePath = "",
            ImageDataField = ""
        };
    }

    // ======================================================
    // ✅ Barcode ファクトリ（AR: Barcode互換）
    // ======================================================
    public static DesignControl CreateBarcode(double leftMm, double topMm)
    {
        return new DesignControl
        {
            Type = "Barcode",
            Name = "Barcode",
            LeftMm = leftMm,
            TopMm = topMm,
            WidthMm = 40,
            HeightMm = 15,
            BarcodeType = "Code128",
            BarcodeValue = "1234567890",
            BarcodeShowText = true,
            BarColor = 0x000000
        };
    }
}
