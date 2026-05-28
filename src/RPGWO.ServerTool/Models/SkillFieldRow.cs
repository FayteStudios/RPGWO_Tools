using System;
using System.Collections.Generic;

namespace RPGWO.ServerTool.Models;

/// <summary>
/// Editable row for one known skill.ini field.
/// Mirrors the simpler item editor row model: known fields are always visible,
/// blank value fields are skipped, checked flags are written.
/// </summary>
public sealed class SkillFieldRow
{
    public required string Group { get; init; }

    public required string Label { get; init; }

    public required string IniKey { get; init; }

    public required string Kind { get; init; }

    public bool Required { get; init; }

    public bool IsCore { get; init; }

    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();

    public string Value { get; set; } = "";

    public bool Enabled { get; set; }

    public bool IsFlag => Kind.Equals("flag", StringComparison.OrdinalIgnoreCase);

    public bool IsValueBool => Kind.Equals("value_bool", StringComparison.OrdinalIgnoreCase);

    public string KnownText => "Known";
}
