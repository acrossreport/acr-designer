using AcrossReportDesigner.Models;

namespace AcrossReportDesigner.Engines
{
    /// <summary>
    /// テキスト寄せ方向
    /// </summary>
    public enum ReportTextAlign
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    /// <summary>
    /// 描画確定ノード（Top / Text / PageIndex / ZIndex が確定している）
    ///
    /// ※ここに入った時点で、フォントに関する「最終決定」は済んでいる前提。
    ///   Rendering/PDF 側は FontMode と EmbedFont を見て実体フォントを選ぶだけ。
    /// </summary>
    public sealed class RenderNode
    {
        public string Type = "";
        public string Name = "";

        public double Left;
        public double Top;
        public double Width;
        public double Height;

        public string Text = "";

        public int ForeColor;
        public int BackColor;
        public int BackStyle;
        public int LineColor;
        public double LineWidth;

        public string FontName = "MS Gothic";
        public double FontSize;

        // ★ 追加：確定済み FontMode
        public FontMode FontMode = FontMode.Default;

        // ★ 追加：PDF 埋め込みフラグ
        public bool EmbedFont;

        public int ZIndex;
        public int PageIndex;

        // 既存のまま
        public ReportTextAlign TextAlign { get; set; } = ReportTextAlign.Left;
    }
}
