namespace RPGWO.Data.Definitions;

/// <summary>
/// Strongly typed representation of one spell from magic.ini.
/// magic.ini uses Spell=&lt;id&gt; as the start of each spell definition.
/// </summary>
public sealed class MagicDefinition : DefinitionBase
{
    public List<string> Flags { get; } = new();

    // Core
    public string? Description { get; set; }

    public string? Skill { get; set; }

    public string? SkillToLearn { get; set; }

    public int? SkillMin { get; set; }

    public int? SkillMax { get; set; }

    public int? WandUse { get; set; }

    public int? ManaCost { get; set; }

    public int? Range { get; set; }

    public string? Target { get; set; }

    public double? CastTime { get; set; }

    public int? SuccessXp { get; set; }

    public int? FailedXp { get; set; }

    // Runes
    public List<string> Runes { get; } = new();

    public List<int> RuneUses { get; } = new();

    // Animations
    public List<int> Animations { get; } = new();

    public string? ProjectileAnimation { get; set; }

    public string? Sound { get; set; }

    // Effects
    public int? Variance { get; set; }

    public int? Life { get; set; }

    public int? LifeRenewal { get; set; }

    public int? LifeSteal { get; set; }

    public int? Stamina { get; set; }

    public int? StaminaRenewal { get; set; }

    public int? StaminaSteal { get; set; }

    public int? Mana { get; set; }

    public int? ManaRenewal { get; set; }

    public int? ManaSteal { get; set; }

    public int? Cure { get; set; }

    public int? Ice { get; set; }

    public int? Blind { get; set; }

    public int? Hero { get; set; }

    public int? Strength { get; set; }

    public int? Dexterity { get; set; }

    public int? Quickness { get; set; }

    public int? Intelligence { get; set; }

    public int? Wisdom { get; set; }

    public int? Armor { get; set; }

    public int? Improve { get; set; }

    public int? EssenceSteal { get; set; }

    public string? DamageType { get; set; }

    // Spawn / transform / golem
    public List<string> SpawnItems { get; } = new();

    public List<int> SpawnItemQuantities { get; } = new();

    public string? TransformFrom { get; set; }

    public string? TransformTo { get; set; }

    public string? GolemItem { get; set; }

    public string? GolemMonster { get; set; }

    public string? GolemSkill { get; set; }

    // Bare flags
    public bool? Perk { get; set; }

    public bool? DeityOnly { get; set; }

    public bool? IgnoreWandPower { get; set; }

    public bool? LineOfSight { get; set; }

    public bool? AllowDefend { get; set; }

    public bool? SpecialFeature { get; set; }

    public bool? NotOnOthersLand { get; set; }

    public bool? LogHistory { get; set; }

    public bool? AllowOnPlot { get; set; }

    public bool? WarpMemorize { get; set; }

    public bool? Warp { get; set; }

    public bool? CreateWarpStone { get; set; }

    public bool? Lock { get; set; }

    public bool? ActivePlayersOnly { get; set; }

    public bool? ManaBond { get; set; }

    public bool? RevealOre { get; set; }
}