using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Result from reading magic.ini into typed spell definitions.
/// </summary>
public sealed class MagicIniReadResult
{
    public MagicIniReadResult(IniDocument document)
    {
        Document = document;
    }

    public IniDocument Document { get; }

    public List<MagicDefinition> Spells { get; } = new();

    public List<UnknownField> GlobalFields { get; } = new();

    public List<string> GlobalFlags { get; } = new();

    public List<DefinitionParseIssue> Issues { get; } = new();

    public bool HasIssues => Issues.Count > 0;

    public int UnknownFieldCount => Spells.Sum(spell => spell.UnknownFields.Count);

    public int FlagCount => Spells.Sum(spell => spell.Flags.Count);
}