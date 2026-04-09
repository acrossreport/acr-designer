using AcrossReportDesigner.Rendering;
using Avalonia.Media.Imaging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace AcrossReportDesigner.Viewer;

public sealed class ViewerEngine
{
    private readonly List<RenderedPage> _pages = new();
    private int _pageIndex;
    private double _zoom = 1.0;
    public int PageIndex => _pageIndex;
    public int PageCount => _pages.Count;
    public double Zoom => _zoom;
    public event Action? StateChanged;

    public void SetPages(List<RenderedPage> pages)
    {
        _pages.Clear();
        if (pages != null) _pages.AddRange(pages);

        _pageIndex = 0;
        _zoom = 1.0;
        StateChanged?.Invoke();
    }
    public bool CanPrev => _pageIndex > 0;
    public bool CanNext => _pageIndex < _pages.Count - 1;
    public void Prev()
    {
        if (!CanPrev) return;
        _pageIndex--;
        StateChanged?.Invoke();
    }
    public void Next()
    {
        if (!CanNext) return;
        _pageIndex++;
        StateChanged?.Invoke();
    }
    public void SetZoom(double zoom)
    {
        if (zoom < 0.1) zoom = 0.1;
        if (zoom > 8.0) zoom = 8.0;
        _zoom = zoom;
        StateChanged?.Invoke();
    }
    public Bitmap? GetAvaloniaBitmapCurrent()
    {
        if (_pages.Count == 0) return null;

        var src = _pages[_pageIndex].Bitmap;

        // ズーム用にリサイズ（表示専用）
        int w = (int)Math.Max(1, Math.Round(src.Width * _zoom));
        int h = (int)Math.Max(1, Math.Round(src.Height * _zoom));
        using var resized = new SKBitmap(w, h, src.ColorType, src.AlphaType);
        using (var canvas = new SKCanvas(resized))
        {
            canvas.Clear(SKColors.White);
            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            canvas.DrawBitmap(src, new SKRect(0, 0, w, h), paint);
            canvas.Flush();
        }

        return ToAvaloniaBitmap(resized);
    }

    private static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());
        return new Bitmap(ms);
    }
}
