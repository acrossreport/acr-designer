namespace AcrossReportDesigner.Models;
public readonly struct RectMm
{
    public readonly double Left;
    public readonly double Top;
    public readonly double Width;
    public readonly double Height;
    public RectMm(double left, double top, double width, double height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }
    public bool EqualsApprox(RectMm other, double eps = 0.0001)
    {
        return
            System.Math.Abs(Left - other.Left) < eps &&
            System.Math.Abs(Top - other.Top) < eps &&
            System.Math.Abs(Width - other.Width) < eps &&
            System.Math.Abs(Height - other.Height) < eps;
    }
}
