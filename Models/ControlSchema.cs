using System.Collections.Generic;

namespace AcrossReportDesigner.Models;

public sealed class ControlSchema
{
    public string Name { get; set; } = "";
    public List<SchemaProperty> Properties { get; set; } = new();
}

public sealed class SchemaProperty
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string Editor { get; set; } = "textbox";

    public List<string>? ComboItems { get; set; }

    // ★ これが足りなかった
    public List<SchemaProperty> Properties { get; set; } = new();
}