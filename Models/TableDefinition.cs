using System.Collections.Generic;

namespace AcrossReportDesigner.Models;
public sealed class TableDefinition
{
    public List<TableColumnDefinition> Columns { get; set; } = new();
    public List<TableRowDefinition> Rows { get; set; } = new();

    public bool ShowHeader { get; set; } = true;
    public bool ShowFooter { get; set; } = false;
}
public sealed class TableColumnDefinition
{
    public double WidthMm { get; set; } = 20;
}
public enum TableRowType
{
    Header,
    Detail,
    Footer
}
public sealed class TableRowDefinition
{
    public TableRowType RowType { get; set; }
    public double HeightMm { get; set; } = 8;
    public List<TableCellDefinition> Cells { get; set; } = new();
}
public sealed class TableCellDefinition
{
    public List<DesignControl> Controls { get; set; } = new();
    public int ColSpan { get; set; } = 1;
    public int RowSpan { get; set; } = 1;
}
