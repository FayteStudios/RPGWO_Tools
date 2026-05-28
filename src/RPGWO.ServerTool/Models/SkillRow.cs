namespace RPGWO.ServerTool.Models;

/// <summary>
/// List row for one skill.ini block.
/// </summary>
public sealed class SkillRow
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string Purpose { get; init; } = "";

    public string Usable { get; init; } = "";

    public string SkillPoints { get; init; } = "";

    public string Divisor { get; init; } = "";

    public string Summary => $"Purpose: {Purpose}, Usable: {Usable}, SkillPoints: {SkillPoints}, Divisor: {Divisor}";
}
