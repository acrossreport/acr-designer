namespace AcrossReportDesigner.Services;

public static class UnitConverter
{
    public const double Dpi = 96.0;
    public const double TwipsPerInch = 1440.0;
    public const double PointsPerInch = 72.0;
    public const double MmPerInch = 25.4;

    // =========================
    // mm
    // =========================
    public static double MmToPx(double mm)
        => mm * Dpi / MmPerInch;

    public static double PxToMm(double px)
        => px * MmPerInch / Dpi;

    // =========================
    // Twips
    // =========================
    public static double TwipsToPx(double twips)
        => twips * Dpi / TwipsPerInch;

    public static double PxToTwips(double px)
        => px * TwipsPerInch / Dpi;

    public static double TwipsToMm(double twips)
        => twips / TwipsPerInch * MmPerInch;

    public static double MmToTwips(double mm)
        => mm / MmPerInch * TwipsPerInch;

    // =========================
    // Point
    // =========================
    public static double PtToPx(double pt)
        => pt * Dpi / PointsPerInch;

    public static double PxToPt(double px)
        => px * PointsPerInch / Dpi;

    public static double TwipsToPt(double twips)
        => twips / 20.0; // 1pt = 20 twips

    public static double PtToTwips(double pt)
        => pt * 20.0;

}
