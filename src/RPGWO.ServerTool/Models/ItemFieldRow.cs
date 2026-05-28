using System.Collections.Generic;

namespace RPGWO.ServerTool.Models;

public sealed class ItemFieldRow
{
    public string Group { get; init; } = "";
    public string Label { get; init; } = "";
    public string IniKey { get; init; } = "";
    public string Kind { get; init; } = "text"; // text, number, int, combo, flag
    public string Value { get; set; } = "";
    public bool Enabled { get; set; }
    public bool IsCore { get; init; }
    public bool Required { get; init; }
    public bool IsFlag => Kind == "flag";
    public IReadOnlyList<string> Options { get; init; } = new List<string>();
}
