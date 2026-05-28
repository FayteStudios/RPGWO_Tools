using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Result from reading treasure.ini into typed treasure definitions.
/// </summary>
public sealed class TreasureIniReadResult
{
    public TreasureIniReadResult(IniDocument document)
    {
        Document = document;
    }

    public IniDocument Document { get; }

    public List<TreasureDefinition> Treasures { get; } = new();

    public List<UnknownField> GlobalFields { get; } = new();

    public List<string> GlobalFlags { get; } = new();

    public List<DefinitionParseIssue> Issues { get; } = new();

    public bool HasIssues => Issues.Count > 0;

    public int UnknownFieldCount => Treasures.Sum(treasure => treasure.UnknownFields.Count);

    public int FlagCount => Treasures.Sum(treasure => treasure.Flags.Count);
}