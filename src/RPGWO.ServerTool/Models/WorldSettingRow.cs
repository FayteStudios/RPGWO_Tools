using System;

namespace RPGWO.ServerTool.Models;

/// <summary>
/// Editable GUI row for one world.ini setting or bare flag.
/// </summary>
public sealed class WorldSettingRow
{
    public required string Type { get; set; }

    public required string Key { get; set; }

    public string Value { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public int LineNumber { get; set; }

    public bool IsKnown { get; set; }

    public string OriginalText { get; set; } = "";

    public bool IsFlag => Type.Equals("Flag", StringComparison.OrdinalIgnoreCase);

    public string KnownText => IsKnown ? "Known" : "Unknown";
}