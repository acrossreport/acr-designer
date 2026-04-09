
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AcrossReportDesigner.Models;

public class TreeNode
{
    public string Name { get; set; } = "";
    public ObservableCollection<TreeNode> Children { get; } = new ObservableCollection<TreeNode>();
    public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();
    public bool IsExpanded { get; set; }

    public string NodeType { get; set; }

}
