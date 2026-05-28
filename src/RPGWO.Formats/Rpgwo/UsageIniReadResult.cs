using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Result from reading itemuse.ini into typed usage definitions.
/// </summary>
public sealed class UsageIniReadResult
{
    public UsageIniReadResult(IniDocument document)
    {
        Document = document;
    }

    public IniDocument Document { get; }

    public List<UsageDefinition> Usages { get; } = new();

    public List<UnknownField> GlobalFields { get; } = new();

    public List<string> GlobalFlags { get; } = new();

    public List<DefinitionParseIssue> Issues { get; } = new();

    public bool HasIssues => Issues.Count > 0;

    public int UnknownFieldCount => Usages.Sum(usage => usage.UnknownFields.Count);

    public int FlagCount => Usages.Sum(usage => usage.Flags.Count);
}