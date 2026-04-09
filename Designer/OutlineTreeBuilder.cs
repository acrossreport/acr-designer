using Avalonia.Controls;
using AcrossReportDesigner.Models;
using System.Collections.Generic;

namespace AcrossReportDesigner.Designer;

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
            // ✅ アイコン付き表示
            Header = $"{node.Icon} {node.Name}",

            IsExpanded = false,
            Tag = node
        };

        foreach (var child in node.Children)
        {
            item.Items.Add(CreateItem(child));
        }
        return item;
    }
}
