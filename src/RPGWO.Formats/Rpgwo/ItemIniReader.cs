using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Converts legacy RPGWO item.ini lines into typed ItemDefinition objects.
/// RPGWO item.ini uses Item=<id> as the start of each item definition.
/// Bare lines such as Stackable, NotMovable, Lockable, etc. are item flags.
/// </summary>
public static class ItemIniReader
{
    public static ItemIniReadResult ReadFile(string path)
    {
        IniParseResult parseResult = IniParser.ParseFile(path);
        return ReadDocument(parseResult.Document);
    }

    public static ItemIniReadResult ReadDocument(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new ItemIniReadResult(document);

        ItemDefinition? currentItem = null;
        int fallbackId = 0;

        foreach (IniLine line in document.Lines)
        {
            switch (line)
            {
                case IniKeyValueLine keyValueLine:
                    HandleKeyValueLine(
                        keyValueLine,
                        result,
                        ref currentItem,
                        ref fallbackId);
                    break;

                case IniFlagLine flagLine:
                    HandleFlagLine(
                        flagLine,
                        result,
                        currentItem);
                    break;
            }
        }

        return result;
    }

    private static void HandleKeyValueLine(
        IniKeyValueLine line,
        ItemIniReadResult result,
        ref ItemDefinition? currentItem,
        ref int fallbackId)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (IsNewItemMarker(key))
        {
            currentItem = new ItemDefinition
            {
                Id = TryParseInt(value, out int parsedId)
                    ? parsedId
                    : ++fallbackId
            };

            result.Items.Add(currentItem);

            if (!TryParseInt(value, out _))
            {
                var issue = new DefinitionParseIssue(
                    message: "Item marker did not contain a numeric ID. A fallback ID was assigned.",
                    lineNumber: line.LineNumber,
                    key: key,
                    value: value);

                currentItem.Issues.Add(issue);
                result.Issues.Add(issue);
            }

            return;
        }

        if (currentItem is null)
        {
            result.GlobalFields.Add(new UnknownField(
                key: key,
                value: value,
                lineNumber: line.LineNumber,
                originalText: line.OriginalText));

            return;
        }

        ApplyItemField(currentItem, line, result);
    }

    private static void HandleFlagLine(
        IniFlagLine line,
        ItemIniReadResult result,
        ItemDefinition? currentItem)
    {
        string flagName = line.Name.Trim();

        if (string.IsNullOrWhiteSpace(flagName))
            return;

        if (currentItem is null)
        {
            result.GlobalFlags.Add(flagName);
            return;
        }

        currentItem.Flags.Add(flagName);

        switch (flagName.ToUpperInvariant())
        {
            case "STACKABLE":
                currentItem.Stackable = true;
                break;

            case "2HANDWEAPON":
                currentItem.TwoHandWeapon = true;
                break;

            case "OPENSIGHTLINE":
                currentItem.OpenSightLine = true;
                break;

            case "STAMINADAMAGE":
                currentItem.StaminaDamage = true;
                break;

            case "ALWAYSSTOCK":
                currentItem.AlwaysStock = true;
                break;

            case "SHIELDBREAK":
                currentItem.ShieldBreak = true;
                break;

            case "IGNORESHIELDS":
                currentItem.IgnoreShields = true;
                break;

            case "INVISIBLE":
                currentItem.Invisible = true;
                break;

            case "LOCKABLE":
                currentItem.Lockable = true;
                break;

            case "KEYABLE":
                currentItem.Keyable = true;
                break;

            case "READABLE":
                currentItem.Readable = true;
                break;

            case "DESTROYABLE":
                currentItem.Destroyable = true;
                break;

            case "FIXABLE":
                currentItem.Fixable = true;
                break;

            case "NOTCONTAINERABLE":
                currentItem.NotContainerable = true;
                break;

            case "POSTABLE":
                currentItem.Postable = true;
                break;

            case "ANCIENT":
                currentItem.Ancient = true;
                break;

            case "PKDAMAGE":
                currentItem.PkDamage = true;
                break;

            case "BLOCKMOVEMENT":
                currentItem.BlockMovement = true;
                break;

            case "NOTMOVABLE":
                currentItem.NotMovable = true;
                break;

            case "NOTPICKUPABLE":
                currentItem.NotPickupable = true;
                break;

            case "NODROP":
            case "NODEATHDROP":
                currentItem.NoDrop = true;
                break;

            case "NOECONOMYVALUEDROP":
                currentItem.NoEconomyValueDrop = true;
                break;
        }
    }

    private static bool IsNewItemMarker(string key)
    {
        return key.Equals("Item", StringComparison.OrdinalIgnoreCase)
            || key.Equals("ItemID", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyItemField(
        ItemDefinition item,
        IniKeyValueLine line,
        ItemIniReadResult result)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        switch (key.ToUpperInvariant())
        {
            case "NAME":
                item.Name = value;
                break;

            case "IMAGE":
                item.Image = ParseNullableInt(item, line, result);
                break;

            case "ANIMATION":
            case "ANIMATION0":
                item.Animation = ParseNullableInt(item, line, result);
                break;
            case "ANIMATION1":
                item.Animation = ParseNullableInt(item, line, result);
                break;
            case "ANIMATION2":
                item.Animation = ParseNullableInt(item, line, result);
                break;
            case "ANIMATION3":
                item.Animation = ParseNullableInt(item, line, result);
                break;
            case "ANIMATION4":
                item.Animation = ParseNullableInt(item, line, result);
                break;
            case "ANIMATION5":
                item.Animation = ParseNullableInt(item, line, result);
                break;
            case "ANIMATION6":
                item.Animation = ParseNullableInt(item, line, result);
                break;
            case "ANIMATION7":
                item.Animation = ParseNullableInt(item, line, result);
                break;
            case "ANIMATION8":
                item.Animation = ParseNullableInt(item, line, result);
                break;
            case "ANIMATION9":
                item.Animation = ParseNullableInt(item, line, result);
                break;

            case "CLASS":
                item.Class = value;
                break;

            case "TYPE":
                item.Type = value;
                break;

            case "SUBTYPE":
                item.SubType = value;
                break;

            case "BURDEN":
                item.Burden = ParseNullableInt(item, line, result);
                break;

            case "VALUE":
                item.Value = ParseNullableInt(item, line, result);
                break;

            case "STACKLIMIT":
            case "STACK":
                item.StackLimit = ParseNullableInt(item, line, result);
                break;

            case "DURABILITY":
                item.Durability = ParseNullableInt(item, line, result);
                break;

            case "DAMAGE":
                item.Damage = ParseNullableInt(item, line, result);
                break;

            case "ARMORLEVEL":
                item.ArmorLevel = ParseNullableInt(item, line, result);
                break;

            case "WEAPONDAMAGE":
                item.WeaponDamage = ParseNullableInt(item, line, result);
                break;

            case "WEAPONSPEED":
                item.WeaponSpeed = ParseNullableInt(item, line, result);
                break;

            case "EQUIPSLOT":
                item.EquipSlot = value;
                break;

            case "NOTMOVABLE":
                item.NotMovable = ParseNullableBool(item, line, result);
                break;

            case "NOTPICKUPABLE":
                item.NotPickupable = ParseNullableBool(item, line, result);
                break;

            case "NODROP":
            case "NODEATHDROP":
                item.NoDrop = ParseNullableBool(item, line, result);
                break;

            case "NOECONOMYVALUEDROP":
                item.NoEconomyValueDrop = ParseNullableBool(item, line, result);
                break;
            case "GROUP":
                item.Group = value;
                break;

            case "SIZE":
                item.Size = value;
                break;

            case "BREAKID":
                item.BreakId = ParseNullableInt(item, line, result);
                break;

            case "COMBATSKILL":
                item.CombatSkill = value;
                break;

            case "WEARIMAGE":
                item.WearImage = ParseNullableInt(item, line, result);
                break;

            case "ATTACKSPEED":
                item.AttackSpeed = ParseNullableDouble(item, line, result);
                break;
            case "TOTALUSES":
                item.TotalUses = ParseNullableInt(item, line, result);
                break;

            case "WEAPONAL":
                item.WeaponArmorLevel = ParseNullableInt(item, line, result);
                break;

            case "SKILLREQ":
                item.SkillReq = ParseNullableInt(item, line, result);
                break;

            case "ARMORSPOT":
                item.ArmorSpot = value;
                break;

            case "FOOD":
                item.Food = ParseNullableInt(item, line, result);
                break;

            case "TRADERMAX":
                item.TraderMax = ParseNullableInt(item, line, result);
                break;

            case "MAGICARMORLEVEL":
                item.MagicArmorLevel = ParseNullableInt(item, line, result);
                break;

            case "TERRAIN":
                item.Terrain = value;
                break;

            case "FIREAL":
                item.FireArmorLevel = ParseNullableInt(item, line, result);
                break;

            case "BONUSCOUNT":
                item.BonusCount = ParseNullableInt(item, line, result);
                break;

            case "ELECTRICAL":
                item.ElectricArmorLevel = ParseNullableInt(item, line, result);
                break;

            case "COLDAL":
                item.ColdArmorLevel = ParseNullableInt(item, line, result);
                break;

            case "SKILLBONUS":
                item.SkillBonus = value;
                break;

            case "SKILLIDBONUS":
                item.SkillIdBonus = value;
                break;

            case "GROWTHSPROUTCHANCE":
                item.GrowthSproutChance = ParseNullableDouble(item, line, result);
                break;

            case "GROWTHSPROUTITEM":
                item.GrowthSproutItem = ParseNullableInt(item, line, result);
                break;

            case "MAGICBREAKCHANCE":
                item.MagicBreakChance = ParseNullableDouble(item, line, result);
                break;

            case "BUILD":
                item.Build = value;
                break;

            case "CRITICALBONUS":
                item.CriticalBonus = ParseNullableInt(item, line, result);
                break;

            case "MAGICSTABILITY":
                item.MagicStability = ParseNullableDouble(item, line, result);
                break;

            case "RARITY":
                item.Rarity = ParseNullableInt(item, line, result);
                break;
            case "LIGHT":
                item.Light = ParseNullableInt(item, line, result);
                break;
            case "DAMAGELOW":
                item.DamageLow = ParseNullableInt(item, line, result);
                break;

            case "DAMAGEHIGH":
                item.DamageHigh = ParseNullableInt(item, line, result);
                break;

            case "WEAPONDAMAGETYPE":
                item.WeaponDamageType = value;
                break;

            case "FIRECATCH":
                item.FireCatch = ParseNullableBool(item, line, result);
                break;

            case "ESSENCESTEAL":
                item.EssenceSteal = ParseNullableInt(item, line, result);
                break;

            case "BLOCKMOVEMENT":
                item.BlockMovement = ParseNullableBool(item, line, result);
                break;

            case "WEAPONMAXRANGE":
                item.WeaponMaxRange = ParseNullableInt(item, line, result);
                break;

            case "DEGRADEDELTA":
                item.DegradeDelta = ParseNullableInt(item, line, result);
                break;

            case "DEGRADEITEM":
                item.DegradeItem = ParseNullableInt(item, line, result);
                break;

            case "GROWTHDELTA":
                item.GrowthDelta = ParseNullableInt(item, line, result);
                break;

            case "GROWTHITEM":
                item.GrowthItem = ParseNullableInt(item, line, result);
                break;

            case "GROWTHGRASSKILL":
                item.GrowthGrassKill = value;
                break;

            case "GROWTHDEADITEM":
                item.GrowthDeadItem = ParseNullableInt(item, line, result);
                break;

            case "GROWTHDEATHCHANCE":
                item.GrowthDeathChance = ParseNullableDouble(item, line, result);
                break;

            case "GROWTHMASSSPREAD":
                item.GrowthMassSpread = value;
                break;

            case "GROWTHELEVATIONRANGE":
                item.GrowthElevationRange = value;
                break;

            case "GROWTHCROWDING":
                item.GrowthCrowding = ParseNullableInt(item, line, result);
                break;

            case "AMMO":
                item.Ammo = ParseNullableBool(item, line, result);
                break;

            case "MISSLEWEAPON":
                item.MissleWeapon = ParseNullableBool(item, line, result);
                break;

            case "STANDDAMAGE":
                item.StandDamage = ParseNullableBool(item, line, result);
                break;

            case "FOODSTAMINA":
                item.FoodStamina = ParseNullableInt(item, line, result);
                break;

            case "MAGICPOWER":
                item.MagicPower = ParseNullableDouble(item, line, result);
                break;

            case "ATTACKANIMATION":
                item.AttackAnimation = value;
                break;

            case "BREAKDURABILITY":
                item.BreakDurability = ParseNullableInt(item, line, result);
                break;

            case "ARTIFACT":
                item.Artifact = ParseNullableBool(item, line, result);
                break;

            case "ARMORDURABILITY":
                item.ArmorDurability = ParseNullableInt(item, line, result);
                break;

            case "DYNAMICCYCLE":
                item.DynamicCycle = ParseNullableInt(item, line, result);
                break;

            case "IMAGETYPE":
                item.ImageType = value;
                break;

            case "STARTERSKILL":
                item.StarterSkill = value;
                break;

            case "DUNGEON":
                item.DungeonEntries.Add(value);
                break;

            case "TRIGGERID":
                item.TriggerId = ParseNullableInt(item, line, result);
                break;

            case "PROJECTILEANIMATION":
                item.ProjectileAnimation = value;
                break;

            case "STEPONID":
                item.StepOnId = ParseNullableInt(item, line, result);
                break;

            case "ALLOWSURFACEGROWTH":
                item.AllowSurfaceGrowth = ParseNullableBool(item, line, result);
                break;

            case "GROWTHHIGHELEVATION":
                item.GrowthHighElevation = ParseNullableInt(item, line, result);
                break;

            case "GROWTHLOWELEVATION":
                item.GrowthLowElevation = ParseNullableInt(item, line, result);
                break;

            case "POISONDAMAGE":
                item.PoisonDamage = ParseNullableInt(item, line, result);
                break;

            case "SPAWNMONSTER":
                item.SpawnMonster = ParseNullableInt(item, line, result);
                break;

            case "SPAWNMONSTERCHANCE":
                item.SpawnMonsterChance = ParseNullableDouble(item, line, result);
                break;

            case "TRAPEFFECT":
                item.TrapEffect = value;
                break;

            case "WEAPONDURABILITY":
                item.WeaponDurability = ParseNullableInt(item, line, result);
                break;

            case "MINESKILLREQ":
                item.MineSkillReq = ParseNullableInt(item, line, result);
                break;

            case "SPAWNMONSTERTIMEOUT":
                item.SpawnMonsterTimeout = ParseNullableInt(item, line, result);
                break;

            case "GROWTHSPROUTRADIUS":
                item.GrowthSproutRadius = ParseNullableInt(item, line, result);
                break;
            case "MAGICBREAKITEMID":
                item.MagicBreakItemId = ParseNullableInt(item, line, result);
                break;

            case "DATA1":
                item.Data1 = value;
                break;

            case "DATA2":
                item.Data2 = value;
                break;

            case "HOLDDAMAGE":
                item.HoldDamage = ParseNullableInt(item, line, result);
                break;

            case "FISHDEPTH":
                item.FishDepth = value;
                break;

            case "WARMTHRADIUS":
                item.WarmthRadius = ParseNullableInt(item, line, result);
                break;

            case "COOLNESS":
                item.Coolness = ParseNullableInt(item, line, result);
                break;

            case "DYNAMICDAMAGE":
                item.DynamicDamage = value;
                break;

            case "MAGICBREAKDAMAGE":
                item.MagicBreakDamage = ParseNullableInt(item, line, result);
                break;

            case "SELFREPAIR":
                item.SelfRepair = ParseNullableInt(item, line, result);
                break;

            case "FLAGDOWN":
                item.FlagDown = ParseNullableInt(item, line, result);
                break;

            case "INVASIONID":
                item.InvasionId = ParseNullableInt(item, line, result);
                break;

            case "WEAPONMINRANGE":
                item.WeaponMinRange = ParseNullableInt(item, line, result);
                break;

            case "WATER":
                item.Water = ParseNullableInt(item, line, result);
                break;

            case "FLAGUP":
                item.FlagUp = ParseNullableInt(item, line, result);
                break;

            case "FOODLIFE":
                item.FoodLife = ParseNullableInt(item, line, result);
                break;

            case "FOODMANA":
                item.FoodMana = ParseNullableInt(item, line, result);
                break;

            case "WARMTH":
                item.Warmth = ParseNullableInt(item, line, result);
                break;

            case "POISONCURE":
                item.PoisonCure = ParseNullableInt(item, line, result);
                break;

            case "DEXTERITYBONUS":
                item.DexterityBonus = ParseNullableInt(item, line, result);
                break;

            case "INTELLIGENCEBONUS":
                item.IntelligenceBonus = ParseNullableInt(item, line, result);
                break;

            case "QUICKNESSBONUS":
                item.QuicknessBonus = ParseNullableInt(item, line, result);
                break;

            case "STRENGTHBONUS":
                item.StrengthBonus = ParseNullableInt(item, line, result);
                break;

            case "WISDOMBONUS":
                item.WisdomBonus = ParseNullableInt(item, line, result);
                break;

            case "WRITING":
                item.Writing = value;
                break;

            case "STARTERQTY":
                item.StarterQty = ParseNullableInt(item, line, result);
                break;

            case "THRUSTAL":
                item.ThrustArmorLevel = ParseNullableInt(item, line, result);
                break;

            case "BASHAL":
                item.BashArmorLevel = ParseNullableInt(item, line, result);
                break;

            case "CUTAL":
                item.CutArmorLevel = ParseNullableInt(item, line, result);
                break;

            case "BLOOD":
                item.Blood = value;
                break;

            case "RESTGAIN":
                item.RestGain = ParseNullableInt(item, line, result);
                break;

            case "SCANABLE":
                item.Scanable = ParseNullableInt(item, line, result);
                break;

            case "SCANANIMATION":
                item.ScanAnimation = value;
                break;

            case "BOUNCE":
                item.Bounce = ParseNullableInt(item, line, result);
                break;

            case "BUILDWARP":
                item.BuildWarp = value;
                break;

            case "DUNGEONWARP":
                item.DungeonWarp = value;
                break;

            case "EXCLUDEITEM":
                item.ExcludeItem = ParseNullableInt(item, line, result);
                break;

            case "MOVEDIRECTION":
                item.MoveDirection = value;
                break;

            case "DATA3":
                item.Data3 = value;
                break;

            case "DATA4":
                item.Data4 = value;
                break;

            case "DAYID":
                item.DayId = ParseNullableInt(item, line, result);
                break;

            case "NITEID":
                item.NiteId = ParseNullableInt(item, line, result);
                break;

            case "DUNGEONADDSIZE":
                item.DungeonAddSize = ParseNullableInt(item, line, result);
                break;

            case "DUNGEONSURFACE":
                item.DungeonSurface = ParseNullableInt(item, line, result);
                break;

            case "ITEMSPAWN":
                item.ItemSpawn = ParseNullableInt(item, line, result);
                break;

            case "ITEMSPAWNDELTA":
                item.ItemSpawnDelta = ParseNullableInt(item, line, result);
                break;

            case "POISONRATE":
                item.PoisonRate = ParseNullableInt(item, line, result);
                break;

            case "STEALTHVISION":
                item.StealthVision = ParseNullableInt(item, line, result);
                break;


            default:
                item.UnknownFields.Add(new UnknownField(
                    key: key,
                    value: value,
                    lineNumber: line.LineNumber,
                    originalText: line.OriginalText));
                break;
        }
    }

    private static double? ParseNullableDouble(
    ItemDefinition item,
    IniKeyValueLine line,
    ItemIniReadResult result)
    {
        string value = line.Value.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (double.TryParse(value, out double parsed))
            return parsed;

        var issue = new DefinitionParseIssue(
            message: "Expected decimal number value.",
            lineNumber: line.LineNumber,
            key: line.Key,
            value: line.Value);

        item.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }
    private static int? ParseNullableInt(
        ItemDefinition item,
        IniKeyValueLine line,
        ItemIniReadResult result)
    {
        string value = line.Value.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, out int parsed))
            return parsed;

        var issue = new DefinitionParseIssue(
            message: "Expected integer value.",
            lineNumber: line.LineNumber,
            key: line.Key,
            value: line.Value);

        item.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool? ParseNullableBool(
        ItemDefinition item,
        IniKeyValueLine line,
        ItemIniReadResult result)
    {
        string value = line.Value.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (bool.TryParse(value, out bool parsedBool))
            return parsedBool;

        if (int.TryParse(value, out int parsedInt))
            return parsedInt != 0;

        if (value.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("no", StringComparison.OrdinalIgnoreCase))
            return false;

        var issue = new DefinitionParseIssue(
            message: "Expected boolean value.",
            lineNumber: line.LineNumber,
            key: line.Key,
            value: line.Value);

        item.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool TryParseInt(string value, out int parsed)
    {
        return int.TryParse(value.Trim(), out parsed);
    }
}