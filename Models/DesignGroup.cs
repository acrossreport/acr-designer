using System.Collections.Generic;

namespace AcrossReportDesigner.Models;

public sealed class DesignGroup
{
    public string Name { get; set; } = "Group";

    // グループに属するコントロール
    public List<DesignControl> Controls { get; } = new();
}
