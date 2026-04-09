using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace AcrossReportDesigner.Designer
{
    public sealed class ZoomController
    {
        private readonly Control _target;
        private readonly ScrollViewer _scroll;
        private readonly TextBlock _zoomText;

        private readonly ScaleTransform _scale = new ScaleTransform(1, 1);
        private double _zoom = 1.0;

        private const double ZoomMin = 0.5;
        private const double ZoomMax = 5.0;

        public ZoomController(
            Control target,
            ScrollViewer scroll,
            TextBlock zoomText)
        {
            _target = target;
            _scroll = scroll;
            _zoomText = zoomText;

            _target.RenderTransformOrigin =
                new RelativePoint(0, 0, RelativeUnit.Relative);

            _target.RenderTransform = _scale;
        }

        public void HandleWheel(PointerWheelEventArgs e)
        {
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
                return;

            double factor = e.Delta.Y > 0 ? 1.1 : 0.9;
            double newZoom = _zoom * factor;
            newZoom = Math.Clamp(newZoom, ZoomMin, ZoomMax);

            if (Math.Abs(newZoom - _zoom) < 0.0001)
                return;

            // ズーム前のマウス位置（ScrollViewer基準）
            var mousePos = e.GetPosition(_scroll);

            // スクロール位置 + マウス位置 = コンテンツ上の絶対座標
            double contentX = (_scroll.Offset.X + mousePos.X) / _zoom;
            double contentY = (_scroll.Offset.Y + mousePos.Y) / _zoom;

            _zoom = newZoom;
            _scale.ScaleX = _zoom;
            _scale.ScaleY = _zoom;

            // ズーム後も同じコンテンツ座標がマウス位置に来るようスクロール調整
            double newOffsetX = contentX * _zoom - mousePos.X;
            double newOffsetY = contentY * _zoom - mousePos.Y;
            _scroll.Offset = new Vector(
                Math.Max(0, newOffsetX),
                Math.Max(0, newOffsetY));

            _zoomText.Text = $"{System.Math.Round(_zoom * 100)}%";
            e.Handled = true;
        }
    }
}
