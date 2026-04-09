namespace AcrossReportDesigner.Models;

public enum PaperOrientation
{
    Portrait,
    Landscape
}

public enum PaperKind
{
    A4,
    B4
}

public class DesignerSettings
{
    // 用紙
    public PaperKind PaperKind { get; set; } = PaperKind.A4;

    // 基本サイズ（mm）…Portrait想定のベース
    public double PaperWidthMm { get; set; } = 210;
    public double PaperHeightMm { get; set; } = 297;

    // 向き
    public PaperOrientation Orientation { get; set; } = PaperOrientation.Portrait;

    // グリッド
    public double GridMm { get; set; } = 10;

    public void ApplyPaperKind()
    {
        // A4: 210 x 297
        // B4: 257 x 364
        switch (PaperKind)
        {
            case PaperKind.B4:
                PaperWidthMm = 257;
                PaperHeightMm = 364;
                break;

            default:
                PaperWidthMm = 210;
                PaperHeightMm = 297;
                break;
        }
    }
}
