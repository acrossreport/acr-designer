using Avalonia.Controls;
using System.Collections.Generic;

using AcrossReportDesigner.Models; // ✅これが必要

namespace AcrossReportDesigner;

public static class OutlineTreeBuilder
{
    public static void Build(TreeView tree, List<OutlineNode> roots)
    {
        tree.Items.Clear();

        foreach (var root in roots)
        {
            tree.Items.Add(CreateItem(root));
        }
    }

    private static TreeViewItem CreateItem(OutlineNode node)
    {
        var item = new TreeViewItem
        {
            Header = $"{node.Icon} {node.Name}",
            Tag = node,
            IsExpanded = false
        };

        foreach (var child in node.Children)
        {
            item.Items.Add(CreateItem(child));
        }

        return item;
    }
}
