// ============================================================
// AcrEngine.cs — ACR エンジン DLL の C# ラッパー
// ============================================================

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AcrossReportDesigner.Services;

public sealed class AcrEngine : IDisposable
{
    private const string DllName = "axr_runtime";

    // ★ 全引数を UTF-8 で渡す（CharSet.Ansi=Shift-JIS は使わない）

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr acr_render_pdf(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string templateJson,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string dataJson,
        out int outLen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr acr_render_zip(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string templateJson,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string dataJson,
        out int outLen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr acr_render_page_png(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string templateJson,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string dataJson,
        int pageIndex,
        out int outLen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int acr_get_page_count(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string templateJson,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string dataJson);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void acr_free_buffer(IntPtr ptr, int len);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr acr_version();

    // ============================================================
    // 公開API
    // ============================================================

    public static string Version
    {
        get
        {
            var ptr = acr_version();
            return ptr == IntPtr.Zero ? "unknown" : Marshal.PtrToStringUTF8(ptr) ?? "unknown";
        }
    }

    public byte[] RenderPdf(string templateJson, string dataJson)
    {
        var ptr = acr_render_pdf(templateJson, dataJson, out int len);
        return ExtractAndFree(ptr, len, nameof(RenderPdf));
    }

    public byte[] RenderZip(string templateJson, string dataJson)
    {
        var ptr = acr_render_zip(templateJson, dataJson, out int len);
        return ExtractAndFree(ptr, len, nameof(RenderZip));
    }

    public byte[] RenderPagePng(string templateJson, string dataJson, int pageIndex = 0)
    {
        var ptr = acr_render_page_png(templateJson, dataJson, pageIndex, out int len);
        return ExtractAndFree(ptr, len, nameof(RenderPagePng));
    }

    public int GetPageCount(string templateJson, string dataJson)
    {
        return acr_get_page_count(templateJson, dataJson);
    }

    public void SavePdf(string templateJson, string dataJson, string outputPath)
    {
        File.WriteAllBytes(outputPath, RenderPdf(templateJson, dataJson));
    }

    public void SaveZip(string templateJson, string dataJson, string outputPath)
    {
        File.WriteAllBytes(outputPath, RenderZip(templateJson, dataJson));
    }

    public void RenderPdfFromFiles(string templatePath, string dataPath, string outputPath)
    {
        var tmpl = File.ReadAllText(templatePath, System.Text.Encoding.UTF8);
        var data = File.ReadAllText(dataPath, System.Text.Encoding.UTF8);
        SavePdf(tmpl, data, outputPath);
    }

    private static byte[] ExtractAndFree(IntPtr ptr, int len, string callerName)
    {
        if (ptr == IntPtr.Zero || len <= 0)
            throw new AcrEngineException(
                $"{callerName} failed: engine returned null (len={len})");
        try
        {
            var result = new byte[len];
            Marshal.Copy(ptr, result, 0, len);
            return result;
        }
        finally
        {
            acr_free_buffer(ptr, len);
        }
    }

    public void Dispose() { }
}

public sealed class AcrEngineException : Exception
{
    public AcrEngineException(string message) : base(message) { }
    public AcrEngineException(string message, Exception inner) : base(message, inner) { }
}