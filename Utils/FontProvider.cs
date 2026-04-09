using System.Runtime.InteropServices;
using Avalonia.Media;

namespace AcrossReportDesigner;

/// <summary>
/// ✅ OS別に最適な日本語フォントを自動選択する共通プロバイダ
///
/// Designer / Viewer / Engine 全てで共通利用する
/// </summary>
public static class FontProvider
{
    /// <summary>
    /// ✅ OSに応じたデフォルト日本語ゴシックフォントを返す
    /// App.axaml.cs から Resources["DefaultFontFamily"] に注入する
    /// </summary>
    public static FontFamily GetDefaultJapaneseFont()
    {
        // =========================
        // ✅ Windows
        // =========================
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows標準で最も安全
            return new FontFamily(
                "Yu Gothic UI, MS Gothic"
            );
        }

        // =========================
        // ✅ macOS
        // =========================
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOSはヒラギノが最優先
            return new FontFamily(
                "Hiragino Kaku Gothic ProN, Hiragino Sans, Yu Gothic"
            );
        }

        // =========================
        // ✅ Linux
        // =========================
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // LinuxはNotoが標準（Ubuntuならほぼ入っている）
            return new FontFamily(
                "Noto Sans CJK JP, DejaVu Sans"
            );
        }

        // =========================
        // ✅ 最終Fallback（内蔵Fonts）
        // =========================
        // どのOSでも最後にこれが効く
        return new FontFamily(
            "./Fonts/NotoSansJP-Regular.ttf"
        );
    }

    /// <summary>
    /// ✅ 明朝用（必要なら）
    /// </summary>
    public static FontFamily GetDefaultJapaneseSerifFont()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new FontFamily(
                "Yu Mincho, MS Mincho"
            );
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new FontFamily(
                "Hiragino Mincho ProN, Yu Mincho"
            );
        }

        return new FontFamily(
            "./Fonts/NotoSerifJP-Regular.ttf"
        );
    }
}
