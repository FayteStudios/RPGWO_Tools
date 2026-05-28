using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Result from reading multiuse.ini into typed multi-use recipe definitions.
/// </summary>
public sealed class MultiUseIniReadResult
{
    public MultiUseIniReadResult(IniDocument document)
    {
        Document = document;
    }

    public IniDocument Document { get; }

    public List<MultiUseDefinition> Recipes { get; } = new();

    public List<UnknownField> GlobalFields { get; } = new();

    public List<string> GlobalFlags { get; } = new();

    public List<DefinitionParseIssue> Issues { get; } = new();

    public bool HasIssues => Issues.Count > 0;

    public int UnknownFieldCount => Recipes.Sum(recipe => recipe.UnknownFields.Count);

    public int FlagCount => Recipes.Sum(recipe => recipe.Flags.Count);
}