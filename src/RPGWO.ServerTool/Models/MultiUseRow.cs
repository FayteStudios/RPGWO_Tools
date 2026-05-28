namespace RPGWO.ServerTool.Models;

/// <summary>
/// List row for one multiuse.ini recipe block.
/// </summary>
public sealed class MultiUseRow
{
    public required int Id { get; init; }

    public string SuccessItem { get; init; } = "";

    public string FocusItem { get; init; } = "";

    public string Skill { get; init; } = "";

    public int FieldCount { get; init; }

    public int FlagCount { get; init; }

    public int UnknownCount { get; init; }

    public int IssueCount { get; init; }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SuccessItem))
                return SuccessItem;

            if (!string.IsNullOrWhiteSpace(FocusItem))
                return FocusItem;

            return $"Recipe {Id}";
        }
    }

    public string Summary =>
        $"Success: {SuccessItem}, Focus: {FocusItem}, Skill: {Skill}, Fields: {FieldCount}, Flags: {FlagCount}, Unknowns: {UnknownCount}, Issues: {IssueCount}";
}