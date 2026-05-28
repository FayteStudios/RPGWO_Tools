namespace RPGWO.Data.Definitions;

/// <summary>
/// Strongly typed representation of one item from item.ini.
/// This will grow as we confirm RPGWO item fields.
/// </summary>
public sealed class ItemDefinition : DefinitionBase
{
    public List<string> Flags { get; } = new();

    public int? Image { get; set; }

    public int? Animation { get; set; }

    public string? Class { get; set; }

    public string? Type { get; set; }

    public string? SubType { get; set; }

    public int? Burden { get; set; }

    public int? Value { get; set; }

    public int? StackLimit { get; set; }

    public int? Durability { get; set; }

    public int? Damage { get; set; }

    public int? ArmorLevel { get; set; }

    public string? EquipSlot { get; set; }

    // Common item.ini fields from real server data.
    public string? Group { get; set; }

    public string? Size { get; set; }

    public int? BreakId { get; set; }

    public string? CombatSkill { get; set; }

    public int? WearImage { get; set; }
    // Usage / durability / economy.
    public int? TotalUses { get; set; }

    public int? TraderMax { get; set; }

    public int? Rarity { get; set; }

    // Armor / resistance values.
    public int? WeaponArmorLevel { get; set; }

    public int? MagicArmorLevel { get; set; }

    public int? FireArmorLevel { get; set; }

    public int? ElectricArmorLevel { get; set; }

    public int? ColdArmorLevel { get; set; }

    // Skill requirements and bonuses.
    public int? SkillReq { get; set; }

    public int? BonusCount { get; set; }

    public string? SkillBonus { get; set; }

    public string? SkillIdBonus { get; set; }

    public double? CriticalBonus { get; set; }

    // World/build/terrain.
    public string? Terrain { get; set; }

    public string? Build { get; set; }

    public int? Light { get; set; }

    // Food / consumable.
    public int? Food { get; set; }

    // Magic / stability.
    public double? MagicBreakChance { get; set; }

    public double? MagicStability { get; set; }

    // Growth sprouting.
    public double? GrowthSproutChance { get; set; }

    public int? GrowthSproutItem { get; set; }

    // Additional animations.
    public int? Animation0 { get; set; }

    public int? Animation1 { get; set; }

    public int? Animation2 { get; set; }

    public int? Animation3 { get; set; }

    public int? Animation4 { get; set; }

    public int? Animation5 { get; set; }

    public int? Animation6 { get; set; }

    public int? Animation7 { get; set; }

    public int? Animation8 { get; set; }

    public int? Animation9 { get; set; }

    // Armor placement can be numeric or named depending on server data,
    // so preserve it as text for now.
    public string? ArmorSpot { get; set; }
    public double? AttackSpeed { get; set; }

    public int? DamageLow { get; set; }

    public int? DamageHigh { get; set; }

    public string? WeaponDamageType { get; set; }

    public bool? FireCatch { get; set; }

    public int? EssenceSteal { get; set; }

    public bool? BlockMovement { get; set; }

    public int? WeaponMaxRange { get; set; }

    public int? DegradeDelta { get; set; }

    public int? DegradeItem { get; set; }

    public int? GrowthDelta { get; set; }

    public int? GrowthItem { get; set; }

    public string? GrowthGrassKill { get; set; }

    public int? GrowthDeadItem { get; set; }

    public double? GrowthDeathChance { get; set; }

    public string? GrowthMassSpread { get; set; }

    public string? GrowthElevationRange { get; set; }

    public int? GrowthCrowding { get; set; }

    public bool? Ammo { get; set; }

    public bool? MissleWeapon { get; set; }

    public int? WeaponDamage { get; set; }

    public double? WeaponSpeed { get; set; }

    // Promoted bare flags.
    public bool? Stackable { get; set; }

    public bool? TwoHandWeapon { get; set; }

    public bool? OpenSightLine { get; set; }

    public bool? StaminaDamage { get; set; }

    public bool? AlwaysStock { get; set; }

    public bool? ShieldBreak { get; set; }

    public bool? IgnoreShields { get; set; }

    public bool? Invisible { get; set; }

    public bool? Lockable { get; set; }

    public bool? Keyable { get; set; }

    public bool? Readable { get; set; }

    public bool? Destroyable { get; set; }

    public bool? Fixable { get; set; }

    public bool? NotContainerable { get; set; }

    public bool? Postable { get; set; }

    public bool? Ancient { get; set; }

    public bool? PkDamage { get; set; }

    public bool? NotMovable { get; set; }

    public bool? NotPickupable { get; set; }

    public bool? NoDrop { get; set; }

    public bool? NoEconomyValueDrop { get; set; }

    // Damage / combat / magic.
    public bool? StandDamage { get; set; }

    public int? FoodStamina { get; set; }

    public double? MagicPower { get; set; }

    public string? AttackAnimation { get; set; }

    public int? BreakDurability { get; set; }

    public int? ArmorDurability { get; set; }

    public int? WeaponDurability { get; set; }

    public int? PoisonDamage { get; set; }

    // Artifact / starter / metadata.
    public bool? Artifact { get; set; }

    public string? ImageType { get; set; }

    public string? StarterSkill { get; set; }

    public List<string> DungeonEntries { get; } = new();

    // Dynamic / trigger / projectile / trap.
    public int? DynamicCycle { get; set; }

    public int? TriggerId { get; set; }

    public string? ProjectileAnimation { get; set; }

    public int? StepOnId { get; set; }

    public string? TrapEffect { get; set; }

    // Growth / environment.
    public bool? AllowSurfaceGrowth { get; set; }

    public int? GrowthHighElevation { get; set; }

    public int? GrowthLowElevation { get; set; }

    public int? GrowthSproutRadius { get; set; }

    // Monster spawning.
    public int? SpawnMonster { get; set; }

    public double? SpawnMonsterChance { get; set; }

    public double? SpawnMonsterTimeout { get; set; }

    // Mining / skills.
    public int? MineSkillReq { get; set; }

    // Break / magic break behavior.
    public int? MagicBreakItemId { get; set; }

    public int? MagicBreakDamage { get; set; }

    // Generic data payloads. Keep as text because these may be script/effect-specific.
    public string? Data1 { get; set; }

    public string? Data2 { get; set; }

    // Damage / repair / range.
    public int? HoldDamage { get; set; }

    public string? DynamicDamage { get; set; }

    public int? SelfRepair { get; set; }

    public int? WeaponMinRange { get; set; }

    // Fishing / temperature / water.
    public string? FishDepth { get; set; }

    public int? WarmthRadius { get; set; }

    public int? Warmth { get; set; }

    public int? Coolness { get; set; }

    public int? Water { get; set; }

    // Flags / invasion.
    public int? FlagDown { get; set; }

    public int? FlagUp { get; set; }

    public int? InvasionId { get; set; }

    // Food / cure.
    public int? FoodLife { get; set; }

    public int? FoodMana { get; set; }

    public int? PoisonCure { get; set; }

    // Stat bonuses.
    public int? StrengthBonus { get; set; }

    public int? DexterityBonus { get; set; }

    public int? QuicknessBonus { get; set; }

    public int? IntelligenceBonus { get; set; }

    public int? WisdomBonus { get; set; }

    // Written/readable content marker.
    public string? Writing { get; set; }

    // More promoted flags.
    public bool? DesertGrow { get; set; }

    public bool? Forest { get; set; }

    public bool? OneAllowed { get; set; }

    // Rare / special-case item fields.
    public int? StarterQty { get; set; }

    public int? ThrustArmorLevel { get; set; }

    public int? BashArmorLevel { get; set; }

    public int? CutArmorLevel { get; set; }

    public string? Blood { get; set; }

    public int? RestGain { get; set; }

    public int? Scanable { get; set; }

    public string? ScanAnimation { get; set; }

    public int? Bounce { get; set; }

    public string? BuildWarp { get; set; }

    public string? DungeonWarp { get; set; }

    public int? ExcludeItem { get; set; }

    public string? MoveDirection { get; set; }

    public string? Data3 { get; set; }

    public string? Data4 { get; set; }

    public int? DayId { get; set; }

    public int? NiteId { get; set; }

    public int? DungeonAddSize { get; set; }

    public int? DungeonSurface { get; set; }

    public int? ItemSpawn { get; set; }

    public int? ItemSpawnDelta { get; set; }

    public int? PoisonRate { get; set; }

    public int? StealthVision { get; set; }

}