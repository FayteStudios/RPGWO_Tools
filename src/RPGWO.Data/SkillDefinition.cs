namespace RPGWO.Data.Definitions;

/// <summary>
/// Strongly typed representation of one skill from skill.ini.
/// RPGWO skill.ini uses Skill=&lt;id&gt; as the start of each skill definition.
/// </summary>
public sealed class SkillDefinition : DefinitionBase
{
    public List<string> Flags { get; } = new();
    public int? SkillId { get; set; }
    public bool? Usable { get; set; }

    public int? SkillPoints { get; set; }

    public bool? Strength { get; set; }

    public bool? Dexterity { get; set; }

    public bool? Quickness { get; set; }

    public bool? Intelligence { get; set; }

    public bool? Wisdom { get; set; }

    public int? Divisor { get; set; }

    public bool? BurdenFactor { get; set; }

    public string? Description { get; set; }

    public string? Purpose { get; set; }

    public bool? SpecialFeature { get; set; }

    public bool? FreeSkill { get; set; }

    public bool? LevelReq { get; set; }

    public bool? ExcludeSkill { get; set; }
}