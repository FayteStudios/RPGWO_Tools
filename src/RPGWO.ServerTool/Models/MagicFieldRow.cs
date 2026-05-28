using System.Collections.Generic;

namespace RPGWO.ServerTool.Models;

public sealed class MagicFieldRow
{
    public required string Label { get; set; }

    public required string IniKey { get; set; }

    public required string Group { get; set; }

    public string Value { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public string Kind { get; set; } = "Text";

    public List<string> Options { get; set; } = new();

    public bool IsFlag => Kind == "Flag";
}