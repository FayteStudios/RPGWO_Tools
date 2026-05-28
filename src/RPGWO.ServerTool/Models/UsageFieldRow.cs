using System;

namespace RPGWO.ServerTool.Models;

/// <summary>
/// Editable row for one field or bare flag inside an itemuse.ini block.
/// </summary>
public sealed class UsageFieldRow
{
    public required string Type { get; set; }

    public required string Key { get; set; }

    public string Value { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public bool IsKnown { get; set; } = true;

    public bool IsFlag => Type.Equals("Flag", StringComparison.OrdinalIgnoreCase);

    public string KnownText => IsKnown ? "Known" : "Unknown";
}