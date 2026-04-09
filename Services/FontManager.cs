using AcrossReportDesigner.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace AcrossReportDesigner.Services
{
    public static class FontManager
    {
        // =========================================================
        // Fonts フォルダ（実行ディレクトリ配下）
        // =========================================================
        private static readonly string FontsDir =
            Path.Combine(AppContext.BaseDirectory, "Fonts");

        // =========================================================
        // FontMode → ファイル名対応表
        // ★ 実在する .ttf に合わせる
        // =========================================================
        private static readonly Dictionary<FontMode, string> FontFiles =
            new()
            {
                { FontMode.NotoSans,       "NotoSansJP-Regular.ttf" },
                { FontMode.NotoSansBold,   "NotoSansJP-Bold.ttf" },
                { FontMode.NotoSerif,      "NotoSerifJP-Regular.ttf" },
                { FontMode.NotoSerifBold,  "NotoSerifJP-Bold.ttf" }
            };

        // =========================================================
        // Typeface キャッシュ
        // =========================================================
        private static readonly Dictionary<FontMode, SKTypeface> Cache =
            new();

        // =========================================================
        // 公開 API
        // =========================================================
        public static SKTypeface GetTypeface(FontMode mode)
        {
            if (Cache.TryGetValue(mode, out var cached))
                return cached;

            if (!FontFiles.TryGetValue(mode, out var fileName))
                throw new InvalidOperationException($"Unknown FontMode: {mode}");

            string path = Path.Combine(FontsDir, fileName);

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"Font file not found: {path}", path);

            var typeface = SKTypeface.FromFile(path);
            Cache[mode] = typeface;

            return typeface;
        }
    }
}
