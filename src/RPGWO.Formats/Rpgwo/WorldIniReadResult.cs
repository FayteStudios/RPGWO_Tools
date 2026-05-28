using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Result from reading world.ini.
/// </summary>
public sealed class WorldIniReadResult
{
    public WorldIniReadResult(IniDocument document)
    {
        Document = document;
    }

    public IniDocument Document { get; }

    public WorldDefinition World { get; } = new()
    {
        Id = 0,
        Name = "World"
    };

    public List<DefinitionParseIssue> Issues { get; } = new();

    public bool HasIssues => Issues.Count > 0;

    public int KnownFieldCount => World.Settings.Count(setting => setting.IsKnown);

    public int UnknownFieldCount => World.UnknownFields.Count;

    public int KnownFlagCount => World.Flags.Count - World.UnknownFlags.Count;

    public int UnknownFlagCount => World.UnknownFlags.Count;
}