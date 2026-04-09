using AcrossReportDesigner.Models;

namespace AcrossReportDesigner.Logic
{
    /// <summary>
    /// FontMode を最終決定するためのロジッククラス
    /// 「人の判断」を排除し、必ず一意に決まるようにする
    /// </summary>
    public static class FontModeResolver
    {
        /// <summary>
        /// テンプレートと DataJSON の指定から
        /// 最終的に使用する FontMode を決定する
        ///
        /// ルール：
        ///  - DataJSON.FontMode == Default
        ///      → テンプレートの FontMode を使用
        ///  - DataJSON.FontMode != Default
        ///      → DataJSON の指定を最優先
        /// </summary>
        public static FontMode Resolve(
            FontMode templateFontMode,
            FontMode dataFontMode)
        {
            if (dataFontMode == FontMode.Default)
            {
                return templateFontMode;
            }

            return dataFontMode;
        }
    }
}
