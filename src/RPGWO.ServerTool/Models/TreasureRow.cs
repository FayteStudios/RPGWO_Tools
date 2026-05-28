namespace RPGWO.ServerTool.Models;

/// <summary>
/// List row for one treasure.ini block.
/// </summary>
public sealed class TreasureRow
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public int ItemCount { get; init; }

    public int FieldCount { get; init; }

    public int FlagCount { get; init; }

    public int UnknownCount { get; init; }

    public int IssueCount { get; init; }

    public string Summary =>
        $"Items: {ItemCount}, Fields: {FieldCount}, Flags: {FlagCount}, Unknowns: {UnknownCount}, Issues: {IssueCount}";
}