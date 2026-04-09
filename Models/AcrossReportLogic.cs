using System.Collections.Generic;

namespace AcrossReportDesigner.Models;

/// <summary>
/// 帳票全体のロジックルート
/// （Designer / Viewer / Renderer 共通）
/// </summary>
public sealed class AcrossReportLogic
{
    /// <summary>
    /// 帳票セクション集合（唯一のルート）
    /// </summary>
    public ArSections Sections { get; set; } = new ArSections();

    /// <summary>
    /// デザイナー用ツリーノード
    /// </summary>
    public List<TreeNode> TreeNodes { get; set; } = new();
}
