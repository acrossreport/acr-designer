using AcrossReportDesigner.Models;

namespace AcrossReportDesigner.Models
{
    /// <summary>
    /// 帳票要素の基底クラス（mm単位）
    ///
    /// このクラスは「DataJSON 側の指定値」を保持するだけ。
    /// 最終確定（Template との合成）は Services.ReportLoader で行う。
    /// </summary>
    public abstract class ReportItem
    {
        /// <summary>
        /// 要素種別（Label / TextBox / Line など）
        /// </summary>
        public string Type { get; set; } = "";

        // =========================
        // 位置（mm）
        // =========================
        public double Left { get; set; }
        public double Top { get; set; }

        // =========================
        // サイズ（mm）
        // =========================
        public double Width { get; set; }
        public double Height { get; set; }

        // =========================
        // 追加項目（ActiveReports再現用）
        // =========================

        /// <summary>
        /// RPX上の名前（例: Label113）
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 属するセクション（PageHeader / Detail / PageFooter など）
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// スタイル文字列（ActiveReports の Style を保持）
        /// </summary>
        public string? Style { get; set; }

        /// <summary>
        /// Border 情報
        /// </summary>
        public BorderInfo Border { get; set; } = new();

        // =========================
        // ★ 追加：Text（ないと ReportLoader で参照できずコンパイルエラーになる）
        // =========================

        /// <summary>
        /// テキスト（TextBox / Label 等で使用）
        /// Text を持たない要素では null のままで良い。
        /// </summary>
        public string? Text { get; set; }

        // =========================
        // ★ 追加：FontMode（DataJSON 側の指定）
        // =========================

        /// <summary>
        /// DataJSON 側で指定されるフォントモード
        /// Default の場合はテンプレ既定に委ねる。
        /// </summary>
        public FontMode FontMode { get; set; } = FontMode.Default;

        /// <summary>
        /// PDF にフォントを埋め込むかどうか（DataJSON 側指定）
        /// true の場合、PDF 出力段階で Noto に寄せる。
        /// </summary>
        public bool EmbedFont { get; set; } = false;
    }
}
