using System;
using System.Runtime.InteropServices;

namespace AcrossReportDesigner.Engines
{
    /// <summary>
    /// AcrHtmlEngine Rust DLL ラッパー
    /// acr_html_engine.dll を呼び出してHTMLを生成する
    /// </summary>
    public static class AcrHtmlEngine
    {
        private const string DllName = "axr_layout";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr render_html(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string templateJson,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string dataJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_string(IntPtr ptr);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr html_engine_version();

        /// <summary>
        /// テンプレートJSON + データJSON → HTML文字列
        /// </summary>
        public static string RenderHtml(string templateJson, string dataJson)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = render_html(templateJson, dataJson);
                return Marshal.PtrToStringUTF8(ptr) ?? "";
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    free_string(ptr);
            }
        }

        /// <summary>
        /// DLLバージョン取得
        /// </summary>
        public static string Version
        {
            get
            {
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    ptr = html_engine_version();
                    return Marshal.PtrToStringUTF8(ptr) ?? "";
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                        free_string(ptr);
                }
            }
        }
    }
}
