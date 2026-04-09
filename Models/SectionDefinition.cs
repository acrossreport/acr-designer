using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AcrossReportDesigner.Models
{
    public class SectionDefinition
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";

        public double HeightMm { get; set; }

        public List<DesignControl> Controls { get; set; } = new();

        // ★ JSON 元ノード保持
        public JsonNode? SourceNode { get; set; }

        // グループ関連
        public int GroupLevel { get; set; }
        public string? GroupKeyField { get; set; }
        public bool GroupNewPage { get; set; }
        public bool RepeatOnNewPage { get; set; }
        public bool KeepTogether { get; set; }

        // ACR.Detail 拡張
        public int RowsPerPage { get; set; } = 0;

        // ★ 2引数コンストラクタ復元
        public SectionDefinition(string name, string kind)
        {
            Name = name;
            Kind = kind;
        }

        // デフォルト
        public SectionDefinition()
        {
        }
    }
}