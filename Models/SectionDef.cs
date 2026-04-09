using System.Collections.Generic;

namespace AcrossReportDesigner.Models
{
    /// <summary>
    /// レイアウトエンジン用のセクション定義
    /// UIやDesignerに依存しない純粋データ構造
    /// </summary>
    public sealed class SectionDef
    {
        public string Name = "";
        public SectionKind Kind;
        public int GroupLevel;
        public string GroupKeyField = "";
        public bool GroupNewPage;
        public bool RepeatOnNewPage;
        public bool KeepTogether;
        public bool KeepWithNext;
        public PageBreakMode PageBreak;
        public double Height;

        public int ZIndex;

        public List<ElementDef> Elements = new();
    }

    public sealed class ElementDef
    {
        public string Type = "";
        public string Name = "";
        public double Left;
        public double Top;
        public double Width;
        public double Height;

        public string Text = "";
        public string DataField = "";

        public int ForeColor;
        public int BackColor;
        public int BackStyle;
        public int LineColor;
        public double LineWidth;

        public string FontName = "MS UI Gothic";
        public double FontSize;
        public FontMode FontMode = FontMode.Default;

        public int ZIndex;
    }
}
