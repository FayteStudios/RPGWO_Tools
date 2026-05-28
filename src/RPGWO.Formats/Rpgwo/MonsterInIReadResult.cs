using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Result from reading monster.ini into typed monster definitions.
/// </summary>
public sealed class MonsterIniReadResult
{
    public MonsterIniReadResult(IniDocument document)
    {
        Document = document;
    }

    public IniDocument Document { get; }

    public List<MonsterDefinition> Monsters { get; } = new();

    public List<UnknownField> GlobalFields { get; } = new();

    public List<string> GlobalFlags { get; } = new();

    public List<DefinitionParseIssue> Issues { get; } = new();

    public bool HasIssues => Issues.Count > 0;

    public int UnknownFieldCount => Monsters.Sum(monster => monster.UnknownFields.Count);

    public int FlagCount => Monsters.Sum(monster => monster.Flags.Count);
}