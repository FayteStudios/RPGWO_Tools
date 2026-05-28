using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Result from reading skill.ini into typed skill definitions.
/// </summary>
public sealed class SkillIniReadResult
{
    public SkillIniReadResult(IniDocument document)
    {
        Document = document;
    }

    public IniDocument Document { get; }

    public List<SkillDefinition> Skills { get; } = new();

    public List<UnknownField> GlobalFields { get; } = new();

    public List<string> GlobalFlags { get; } = new();

    public List<DefinitionParseIssue> Issues { get; } = new();

    public bool HasIssues => Issues.Count > 0;

    public int UnknownFieldCount => Skills.Sum(skill => skill.UnknownFields.Count);

    public int FlagCount => Skills.Sum(skill => skill.Flags.Count);
}