using SkiaSharp;

namespace AcrossReportDesigner.Rendering;

public sealed class RenderedPage
{
    public int PageIndex { get; }
    public SKBitmap Bitmap { get; }
    public float Dpi { get; }
    public float WidthPx => Bitmap.Width;
    public float HeightPx => Bitmap.Height;

    public RenderedPage(int pageIndex, SKBitmap bitmap, float dpi)
    {
        PageIndex = pageIndex;
        Bitmap = bitmap;
        Dpi = dpi;
    }
}
