using AcrossReportDesigner.Models;
using AcrossReportDesigner.Engines;
using AcrossReportDesigner.Logic;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AcrossReportDesigner.Services
{
    /// <summary>
    /// 帳票テンプレートおよび DataJSON を読み込み、
    /// 描画用データ（RenderNode）を構築する責務を持つクラス
    ///
    /// ★ FontMode の最終確定はこのクラスで行う
    /// </summary>
    public static class ReportLoader
    {
        // =====================================================
        // テンプレート JSON 読み込み
        // =====================================================
        /// <summary>
        /// 帳票テンプレート（AR Sections）を JSON から読み込む
        /// ※ ここでは FontMode は「テンプレート既定値」として保持される
        /// </summary>
        public static ArSections LoadTemplate(string path)
        {
            string json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<ArSections>(json)
                   ?? new ArSections();
        }

        // =====================================================
        // DataJSON 読み込み
        // =====================================================
        /// <summary>
        /// DataJSON を読み込む
        ///
        /// ※ ここではまだ FontMode は「指定値」のまま
        ///    Default / NotoSans / NotoSerif など
        /// </summary>
        public static Dictionary<string, string> LoadData(string path)
        {
            string json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }

        // =====================================================
        // ★ 追加：TemplateNode + ReportItem → RenderNode
        // =====================================================
        /// <summary>
        /// テンプレートノードと DataJSON 由来の ReportItem から
        /// 描画確定ノード（RenderNode）を生成する
        ///
        /// ★ FontMode はここで必ず確定させる
        /// </summary>
        public static RenderNode BuildRenderNode(
            TemplateNode templateNode,
            ReportItem reportItem)
        {
            var renderNode = new RenderNode
            {
                // -------------------------
                // 基本情報
                // -------------------------
                Type = templateNode.Type.ToString(),
                Name = templateNode.Name,

                // -------------------------
                // 位置・サイズ
                // -------------------------
                Left = templateNode.Left,
                Top = templateNode.Top,
                Width = templateNode.Width,
                Height = templateNode.Height,

                // -------------------------
                // テキスト
                // -------------------------
                Text = reportItem switch
                {
                    // TextItem 等、Text を持つ派生型を想定
                    { } item => item.Text ?? templateNode.Text
                },

                // -------------------------
                // 色・線情報（テンプレ優先）
                // -------------------------
                ForeColor = templateNode.ForeColor,
                BackColor = templateNode.BackColor,
                BackStyle = templateNode.BackStyle,
                LineColor = templateNode.LineColor,
                LineWidth = templateNode.LineWidth,

                // -------------------------
                // フォント情報
                // -------------------------
                FontName = templateNode.FontName,
                FontSize = templateNode.FontSize,

                // ★ ここが今回の核心
                // TemplateNode と ReportItem の FontMode を比較し、
                // 最終的な FontMode を一意に決定する
                FontMode = FontModeResolver.Resolve(
                    templateNode.FontMode,
                    (FontMode)reportItem.FontMode),

                // PDF 埋め込みフラグは DataJSON 側を優先
                EmbedFont = reportItem.EmbedFont,

                // -------------------------
                // 描画制御
                // -------------------------
                ZIndex = templateNode.ZIndex
            };

            return renderNode;
        }
    }
}
