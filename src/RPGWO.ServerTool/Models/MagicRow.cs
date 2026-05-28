namespace RPGWO.ServerTool.Models;

/// <summary>
/// List row for one magic.ini spell block.
/// </summary>
public sealed class MagicRow
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string Skill { get; init; } = "";

    public int RuneCount { get; init; }

    public int FieldCount { get; init; }

    public int FlagCount { get; init; }

    public int UnknownCount { get; init; }

    public int IssueCount { get; init; }

    public string Summary =>
        $"Skill: {Skill}, Runes: {RuneCount}, Fields: {FieldCount}, Flags: {FlagCount}, Unknowns: {UnknownCount}, Issues: {IssueCount}";
}