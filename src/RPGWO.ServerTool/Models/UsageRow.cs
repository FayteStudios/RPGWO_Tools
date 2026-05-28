namespace RPGWO.ServerTool.Models;

/// <summary>
/// List row for one itemuse.ini block.
/// </summary>
public sealed class UsageRow
{
    public required int Id { get; init; }

    public string ItemTool { get; init; } = "";

    public string ItemFocus { get; init; } = "";

    public string Skill { get; init; } = "";

    public int FieldCount { get; init; }

    public int FlagCount { get; init; }

    public int UnknownCount { get; init; }

    public int IssueCount { get; init; }

    public string DisplayName
    {
        get
        {
            string tool = string.IsNullOrWhiteSpace(ItemTool) ? "?" : ItemTool;
            string focus = string.IsNullOrWhiteSpace(ItemFocus) ? "?" : ItemFocus;

            return $"{tool} + {focus}";
        }
    }

    public string Summary =>
        $"Tool: {ItemTool}, Focus: {ItemFocus}, Skill: {Skill}, Fields: {FieldCount}, Flags: {FlagCount}, Unknowns: {UnknownCount}, Issues: {IssueCount}";
}