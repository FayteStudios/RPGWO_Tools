namespace RPGWO.Data.Definitions;

/// <summary>
/// Strongly typed representation of one monster from monster.ini.
/// This starts broad but conservative and will expand as we inspect real server data.
/// </summary>
public sealed class MonsterDefinition : DefinitionBase
{
    public List<string> Flags { get; } = new();

    public int? Image { get; set; }

    public int? Animation { get; set; }

    public int? Animation0 { get; set; }

    public int? Animation1 { get; set; }

    public int? Animation2 { get; set; }

    public int? Animation3 { get; set; }

    public string? Class { get; set; }

    public string? Type { get; set; }

    public string? SubType { get; set; }

    public int? Level { get; set; }

    public int? Life { get; set; }

    public int? Stamina { get; set; }

    public int? Mana { get; set; }

    public int? Strength { get; set; }

    public int? Dexterity { get; set; }

    public int? Quickness { get; set; }

    public int? Intelligence { get; set; }

    public int? Wisdom { get; set; }

    public int? Attack { get; set; }

    public int? Defense { get; set; }

    public int? DamageLow { get; set; }

    public int? DamageHigh { get; set; }

    public int? AttackSpeed { get; set; }

    public string? DamageType { get; set; }

    public int? ArmorLevel { get; set; }

    public int? MagicArmorLevel { get; set; }

    public int? FireArmorLevel { get; set; }

    public int? ColdArmorLevel { get; set; }

    public int? ElectricArmorLevel { get; set; }

    public int? BashArmorLevel { get; set; }

    public int? CutArmorLevel { get; set; }

    public int? ThrustArmorLevel { get; set; }

    public int? Experience { get; set; }

    public int? Money { get; set; }

    public int? Treasure { get; set; }

    public int? MoveSpeed { get; set; }

    public int? SightRange { get; set; }

    public int? ChaseRange { get; set; }

    public int? SpawnTime { get; set; }

    public int? SpawnRange { get; set; }

    public int? TameSkill { get; set; }

    public int? TameDifficulty { get; set; }

    public int? GrowthMonster { get; set; }

    public string? AttackSound { get; set; }

    public string? DefendSound { get; set; }

    public string? DeathSound { get; set; }

    public string? IdleSound { get; set; }

    public bool? NoCorpse { get; set; }

    public bool? NoRespawn { get; set; }

    public bool? Aggressive { get; set; }

    public bool? Passive { get; set; }

    public bool? Tameable { get; set; }

    public bool? Flying { get; set; }

    public bool? Swimming { get; set; }

    // Core combat / defenses.
    public int? MagicDefense { get; set; }

    public int? MeleeDefense { get; set; }

    public int? MissleDefense { get; set; }

    public int? Scan { get; set; }

    public string? Weapon { get; set; }

    public string? Catagory { get; set; }

    public int? Unarmed { get; set; }

    public double? FearFactor { get; set; }

    // Equipment armor references.
    public string? ChestArmor { get; set; }

    public string? HeadArmor { get; set; }

    public string? LegArmor { get; set; }

    // Spell behavior.
    public string? CastSpell { get; set; }

    public string? CastHeal { get; set; }

    // Repeated treasure fields.
    public List<string> Treasures { get; } = new();

    public List<int> TreasureQuantities { get; } = new();

    public List<double> TreasureChances { get; } = new();

    // Category relationships.
    public List<string> FriendCatagories { get; } = new();

    public List<string> EnemyCatagories { get; } = new();

    // Common monster flags.
    public bool? HelpFriends { get; set; }

    public bool? NotTamable { get; set; }

    public bool? LogHistory { get; set; }

    public bool? ScanAlot { get; set; }

    public bool? NotAttackable { get; set; }

    public bool? Desert { get; set; }

    public bool? Unique { get; set; }

    public bool? AttackHigh { get; set; }

    public bool? MoveFast { get; set; }

    public bool? AttackLow { get; set; }

    public bool? ItemDamageImmune { get; set; }

    public bool? AttackMonsters { get; set; }

    public bool? StealthVision { get; set; }

    public bool? TradeAlwaysStock { get; set; }

    public bool? IgnorePlayers { get; set; }

    public bool? StandStill { get; set; }

    public bool? AirMove { get; set; }

    public bool? NoHands { get; set; }

    public bool? AttackMid { get; set; }

    public bool? GhostMove { get; set; }

    public bool? NeedWarmth { get; set; }

    public bool? OnlyBuyLoot { get; set; }

    public bool? WaterMove { get; set; }

    public bool? EatGrass { get; set; }

    public bool? IgnoreNewbies { get; set; }

    // Skills / abilities.
    public int? Run { get; set; }

    public int? Sword { get; set; }

    public string? Sheild { get; set; }

    public int? Dagger { get; set; }

    public int? Bow { get; set; }

    public int? Sneak { get; set; }

    public int? Throwing { get; set; }

    public int? Crossbow { get; set; }

    public string? RangeWeapon { get; set; }

    // Spell casting / magic.
    public string? CastHarm { get; set; }

    public string? CastNova { get; set; }

    public string? CastHero { get; set; }

    public string? CastIce { get; set; }

    public string? CastBlackHole { get; set; }

    public double? MagicPower { get; set; }

    public int? MagicArmorLevelAlt { get; set; }

    // Talk / NPC behavior.
    public string? TalkGreeting { get; set; }

    public string? TalkIdle { get; set; }

    // Visual / death.
    public string? ImageType { get; set; }

    public string? DeadItem { get; set; }

    public bool? Undead { get; set; }

    // Trade.
    public double? TradeBuyValue { get; set; }

    public double? TradeSellValue { get; set; }

    public List<string> TradeGroups { get; } = new();

    // AI behavior.
    public int? KeepDistance { get; set; }

    // Additional skills / combat behavior.
    public int? Stealth { get; set; }

    public int? Axe { get; set; }

    public int? Mace { get; set; }

    public int? Flail { get; set; }

    public int? Scythe { get; set; }

    public double? FastProcess { get; set; }

    public int? Roam { get; set; }

    public string? DamageFragments { get; set; }

    public int? RobPlayer { get; set; }

    // Trade dialogue.
    public string? TradeTalkFarewell { get; set; }

    public string? TradeTalkSuccess { get; set; }

    // Simple friend references.
    public List<string> Friends { get; } = new();

    // Quest fields are highly structured, so preserve as grouped raw strings for now.
    public List<string> QuestTakeItems { get; } = new();

    public List<string> QuestTalkEntries { get; } = new();

    public List<string> QuestGiveItems { get; } = new();

    public List<string> QuestGiveQuantities { get; } = new();

    public List<string> QuestGiveExperience { get; } = new();

    public List<string> QuestGiveData1 { get; } = new();

    public List<string> QuestGiveData2 { get; } = new();

    public List<string> QuestGiveData3 { get; } = new();

    public List<string> QuestGiveData4 { get; } = new();

    // Treasure data extensions such as TreasureData1-4.
    public List<string> TreasureData1 { get; } = new();

    public List<string> TreasureData2 { get; } = new();

    public List<string> TreasureData3 { get; } = new();

    public List<string> TreasureData4 { get; } = new();

    public bool? CastLightning { get; set; }

    public int? Staff { get; set; }

    // Growth / transformation.
    public int? GrowthMonsterChance { get; set; }

    public int? GrowthMonsterTimeout { get; set; }

    public string? IdleTransformItem { get; set; }

    // Item spawning.
    public string? SpawnItem { get; set; }

    public double? SpawnItemChance { get; set; }

    public int? SpawnItemTimeout { get; set; }

    public string? ItemTrail { get; set; }

    // Additional movement / AI.
    public int? RoamChance { get; set; }

    public string? ChaseItem { get; set; }

    public int? WarpMove { get; set; }

    public int? Swim { get; set; }

    // Additional combat skills.
    public int? Spear { get; set; }

    // Animation / greeting.
    public string? GreetingAnimation { get; set; }

    // Quest / trade extras.
    public List<string> QuestTakeQuantities { get; } = new();

    public List<string> QuestGiveTames { get; } = new();

    public List<string> TradeGroupSellMaximums { get; } = new();

    // Treasure extras.
    public List<string> TreasureTotalUses { get; } = new();

    public List<string> TreasureTexts { get; } = new();


}