namespace RPGWO.ServerTool.Models;

/// <summary>
/// List row for one monster.ini block.
/// </summary>
public sealed class MonsterRow
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public int FieldCount { get; init; }

    public int FlagCount { get; init; }

    public int UnknownCount { get; init; }

    public int IssueCount { get; init; }

    public string Summary =>
        $"Fields: {FieldCount}, Flags: {FlagCount}, Unknowns: {UnknownCount}, Issues: {IssueCount}";
}