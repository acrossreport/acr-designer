using SkiaSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AcrossReportDesigner.Rendering
{
    public static class TypefaceResolver
    {
        private static SKTypeface? _cached;

        public static SKTypeface ResolveMsGothic()
        {
            if (_cached != null)
                return _cached;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException("MS Gothic is Windows only");

            var path = @"C:\Windows\Fonts\msgothic.ttc";
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            // ★ フェイス名を明示指定（超重要）
            _cached = SKTypeface.FromFile(path, 1);

            if (_cached == null)
                throw new Exception("Failed to load MS Gothic typeface");

            return _cached;
        }
    }
}
