using System.Globalization;
using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Converts legacy RPGWO monster.ini lines into typed MonsterDefinition objects.
/// This first version assumes Monster=<id> or MonsterID=<id> starts a monster block.
/// Bare lines are preserved as monster flags.
/// </summary>
public static class MonsterIniReader
{
    public static MonsterIniReadResult ReadFile(string path)
    {
        IniParseResult parseResult = IniParser.ParseFile(path);
        return ReadDocument(parseResult.Document);
    }

    public static MonsterIniReadResult ReadDocument(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new MonsterIniReadResult(document);

        MonsterDefinition? currentMonster = null;
        int fallbackId = 0;

        foreach (IniLine line in document.Lines)
        {
            switch (line)
            {
                case IniKeyValueLine keyValueLine:
                    HandleKeyValueLine(
                        keyValueLine,
                        result,
                        ref currentMonster,
                        ref fallbackId);
                    break;

                case IniFlagLine flagLine:
                    HandleFlagLine(
                        flagLine,
                        result,
                        currentMonster);
                    break;
            }
        }

        return result;
    }

    private static void HandleKeyValueLine(
        IniKeyValueLine line,
        MonsterIniReadResult result,
        ref MonsterDefinition? currentMonster,
        ref int fallbackId)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (IsNewMonsterMarker(key))
        {
            currentMonster = new MonsterDefinition
            {
                Id = TryParseInt(value, out int parsedId)
                    ? parsedId
                    : ++fallbackId
            };

            result.Monsters.Add(currentMonster);

            if (!TryParseInt(value, out _))
            {
                var issue = new DefinitionParseIssue(
                    message: "Monster marker did not contain a numeric ID. A fallback ID was assigned.",
                    lineNumber: line.LineNumber,
                    key: key,
                    value: value);

                currentMonster.Issues.Add(issue);
                result.Issues.Add(issue);
            }

            return;
        }

        if (currentMonster is null)
        {
            result.GlobalFields.Add(new UnknownField(
                key: key,
                value: value,
                lineNumber: line.LineNumber,
                originalText: line.OriginalText));

            return;
        }

        ApplyMonsterField(currentMonster, line, result);
    }



    private static void HandleFlagLine(
        IniFlagLine line,
        MonsterIniReadResult result,
        MonsterDefinition? currentMonster)
    {
        string flagName = line.Name.Trim();

        if (string.IsNullOrWhiteSpace(flagName))
            return;

        if (currentMonster is null)
        {
            result.GlobalFlags.Add(flagName);
            return;
        }

        currentMonster.Flags.Add(flagName);

        switch (flagName.ToUpperInvariant())
        {
            case "NOCORPSE":
                currentMonster.NoCorpse = true;
                break;

            case "NORESPAWN":
                currentMonster.NoRespawn = true;
                break;

            case "AGGRESSIVE":
                currentMonster.Aggressive = true;
                break;

            case "PASSIVE":
                currentMonster.Passive = true;
                break;

            case "TAMEABLE":
            case "TAMABLE":
                currentMonster.Tameable = true;
                break;

            case "FLYING":
                currentMonster.Flying = true;
                break;

            case "SWIMMING":
                currentMonster.Swimming = true;
                break;

            case "HELPFRIENDS":
                currentMonster.HelpFriends = true;
                break;

            case "NOTTAMABLE":
            case "NOTTAMEABLE":
                currentMonster.NotTamable = true;
                break;

            case "LOGHISTORY":
                currentMonster.LogHistory = true;
                break;

            case "SCANALOT":
                currentMonster.ScanAlot = true;
                break;

            case "NOTATTACKABLE":
                currentMonster.NotAttackable = true;
                break;

            case "DESERT":
                currentMonster.Desert = true;
                break;

            case "UNIQUE":
                currentMonster.Unique = true;
                break;

            case "ATTACKHIGH":
                currentMonster.AttackHigh = true;
                break;

            case "MOVEFAST":
                currentMonster.MoveFast = true;
                break;

            case "ATTACKLOW":
                currentMonster.AttackLow = true;
                break;

            case "ITEMDAMAGEIMMUNE":
                currentMonster.ItemDamageImmune = true;
                break;

            case "ATTACKMONSTERS":
                currentMonster.AttackMonsters = true;
                break;

            case "STEALTHVISION":
                currentMonster.StealthVision = true;
                break;

            case "TRADEALWAYSSTOCK":
                currentMonster.TradeAlwaysStock = true;
                break;

            case "IGNOREPLAYERS":
                currentMonster.IgnorePlayers = true;
                break;

            case "STANDSTILL":
                currentMonster.StandStill = true;
                break;

            case "AIRMOVE":
                currentMonster.AirMove = true;
                break;

            case "NOHANDS":
                currentMonster.NoHands = true;
                break;

            case "ATTACKMID":
                currentMonster.AttackMid = true;
                break;

            case "GHOSTMOVE":
                currentMonster.GhostMove = true;
                break;

            case "NEEDWARMTH":
                currentMonster.NeedWarmth = true;
                break;

            case "ONLYBUYLOOT":
                currentMonster.OnlyBuyLoot = true;
                break;

            case "WATERMOVE":
                currentMonster.WaterMove = true;
                break;

            case "EATGRASS":
                currentMonster.EatGrass = true;
                break;

            case "IGNORENEWBIES":
                currentMonster.IgnoreNewbies = true;
                break;
        }
    }

    private static bool IsNewMonsterMarker(string key)
    {
        return key.Equals("Monster", StringComparison.OrdinalIgnoreCase)
            || key.Equals("MonsterID", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyMonsterField(
        MonsterDefinition monster,
        IniKeyValueLine line,
        MonsterIniReadResult result)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (TryApplyNumberedMonsterField(monster, line, result))
            return;


        switch (key.ToUpperInvariant())
        {
            case "NAME":
                monster.Name = value;
                break;

            case "IMAGE":
                monster.Image = ParseNullableInt(monster, line, result);
                break;

            case "ANIMATION":
                monster.Animation = ParseNullableInt(monster, line, result);
                break;

            case "ANIMATION0":
                monster.Animation0 = ParseNullableInt(monster, line, result);
                break;

            case "ANIMATION1":
                monster.Animation1 = ParseNullableInt(monster, line, result);
                break;

            case "ANIMATION2":
                monster.Animation2 = ParseNullableInt(monster, line, result);
                break;

            case "ANIMATION3":
                monster.Animation3 = ParseNullableInt(monster, line, result);
                break;

            case "CLASS":
                monster.Class = value;
                break;

            case "TYPE":
                monster.Type = value;
                break;

            case "SUBTYPE":
                monster.SubType = value;
                break;

            case "LEVEL":
                monster.Level = ParseNullableInt(monster, line, result);
                break;

            case "LIFE":
            case "HITS":
            case "HEALTH":
                monster.Life = ParseNullableInt(monster, line, result);
                break;

            case "STAMINA":
                monster.Stamina = ParseNullableInt(monster, line, result);
                break;

            case "MANA":
                monster.Mana = ParseNullableInt(monster, line, result);
                break;

            case "STRENGTH":
            case "STR":
                monster.Strength = ParseNullableInt(monster, line, result);
                break;

            case "DEXTERITY":
            case "DEX":
                monster.Dexterity = ParseNullableInt(monster, line, result);
                break;

            case "QUICKNESS":
            case "QUICK":
                monster.Quickness = ParseNullableInt(monster, line, result);
                break;

            case "INTELLIGENCE":
            case "INT":
                monster.Intelligence = ParseNullableInt(monster, line, result);
                break;

            case "WISDOM":
            case "WIS":
                monster.Wisdom = ParseNullableInt(monster, line, result);
                break;

            case "ATTACK":
                monster.Attack = ParseNullableInt(monster, line, result);
                break;

            case "DEFENSE":
            case "DEFENCE":
                monster.Defense = ParseNullableInt(monster, line, result);
                break;

            case "DAMAGELOW":
                monster.DamageLow = ParseNullableInt(monster, line, result);
                break;

            case "DAMAGEHIGH":
                monster.DamageHigh = ParseNullableInt(monster, line, result);
                break;

            case "ATTACKSPEED":
                monster.AttackSpeed = ParseNullableInt(monster, line, result);
                break;

            case "DAMAGETYPE":
            case "WEAPONDAMAGETYPE":
                monster.DamageType = value;
                break;

            case "ARMORLEVEL":
            case "ARMOURLEVEL":
                monster.ArmorLevel = ParseNullableInt(monster, line, result);
                break;

            case "MAGICARMORLEVEL":
                monster.MagicArmorLevel = ParseNullableInt(monster, line, result);
                break;

            case "FIREAL":
            case "FIREARMORLEVEL":
                monster.FireArmorLevel = ParseNullableInt(monster, line, result);
                break;

            case "COLDAL":
            case "COLDARMORLEVEL":
                monster.ColdArmorLevel = ParseNullableInt(monster, line, result);
                break;

            case "ELECTRICAL":
            case "ELECTRICARMORLEVEL":
                monster.ElectricArmorLevel = ParseNullableInt(monster, line, result);
                break;

            case "BASHAL":
                monster.BashArmorLevel = ParseNullableInt(monster, line, result);
                break;

            case "CUTAL":
                monster.CutArmorLevel = ParseNullableInt(monster, line, result);
                break;

            case "THRUSTAL":
                monster.ThrustArmorLevel = ParseNullableInt(monster, line, result);
                break;

            case "EXPERIENCE":
            case "EXP":
                monster.Experience = ParseNullableInt(monster, line, result);
                break;

            case "MONEY":
                monster.Money = ParseNullableInt(monster, line, result);
                break;

            case "TREASURE":
            case "TREASUREID":
                monster.Treasure = ParseNullableInt(monster, line, result);
                break;

            case "MOVESPEED":
            case "SPEED":
                monster.MoveSpeed = ParseNullableInt(monster, line, result);
                break;

            case "SIGHTRANGE":
            case "SIGHT":
                monster.SightRange = ParseNullableInt(monster, line, result);
                break;

            case "CHASERANGE":
                monster.ChaseRange = ParseNullableInt(monster, line, result);
                break;

            case "SPAWNTIME":
                monster.SpawnTime = ParseNullableInt(monster, line, result);
                break;

            case "SPAWNRANGE":
                monster.SpawnRange = ParseNullableInt(monster, line, result);
                break;

            case "TAMESKILL":
                monster.TameSkill = ParseNullableInt(monster, line, result);
                break;

            case "TAMEDIFFICULTY":
                monster.TameDifficulty = ParseNullableInt(monster, line, result);
                break;

            case "GROWTHMONSTER":
                monster.GrowthMonster = ParseNullableInt(monster, line, result);
                break;

            case "ATTACKSOUND":
                monster.AttackSound = value;
                break;

            case "DEFENDSOUND":
                monster.DefendSound = value;
                break;

            case "DEATHSOUND":
                monster.DeathSound = value;
                break;

            case "IDLESOUND":
                monster.IdleSound = value;
                break;

            case "NOCORPSE":
                monster.NoCorpse = ParseNullableBool(monster, line, result);
                break;

            case "NORESPAWN":
                monster.NoRespawn = ParseNullableBool(monster, line, result);
                break;

            case "AGGRESSIVE":
                monster.Aggressive = ParseNullableBool(monster, line, result);
                break;

            case "PASSIVE":
                monster.Passive = ParseNullableBool(monster, line, result);
                break;

            case "TAMEABLE":
            case "TAMABLE":
                monster.Tameable = ParseNullableBool(monster, line, result);
                break;

            case "FLYING":
                monster.Flying = ParseNullableBool(monster, line, result);
                break;

            case "SWIMMING":
                monster.Swimming = ParseNullableBool(monster, line, result);
                break;

            case "RUN":
                monster.Run = ParseNullableInt(monster, line, result);
                break;

            case "CASTHARM":
                monster.CastHarm = value;
                break;

            case "SWORD":
                monster.Sword = ParseNullableInt(monster, line, result);
                break;

            case "CASTNOVA":
                monster.CastNova = value;
                break;

            case "SHEILD":
            case "SHIELD":
                monster.Sheild = value;
                break;

            case "TALKGREETING":
                monster.TalkGreeting = value;
                break;

            case "MAGICPOWER":
                monster.MagicPower = ParseNullableDouble(monster, line, result);
                break;

            case "MAGICAL":
                monster.MagicArmorLevelAlt = ParseNullableInt(monster, line, result);
                break;

            case "CASTHERO":
                monster.CastHero = value;
                break;

            case "IMAGETYPE":
                monster.ImageType = value;
                break;

            case "CASTICE":
                monster.CastIce = value;
                break;

            case "CASTBLACKHOLE":
                monster.CastBlackHole = value;
                break;

            case "DEADITEM":
                monster.DeadItem = value;
                break;

            case "DAGGER":
                monster.Dagger = ParseNullableInt(monster, line, result);
                break;

            case "TRADEBUYVALUE":
                monster.TradeBuyValue = ParseNullableDouble(monster, line, result);
                break;

            case "TRADESELLVALUE":
                monster.TradeSellValue = ParseNullableDouble(monster, line, result);
                break;
            case "UNDEAD":
                monster.Undead = ParseNullableBool(monster, line, result);
                break;

            case "BOW":
                monster.Bow = ParseNullableInt(monster, line, result);
                break;

            case "SNEAK":
                monster.Sneak = ParseNullableInt(monster, line, result);
                break;

            case "RANGEWEAPON":
                monster.RangeWeapon = value;
                break;

            case "TALKIDLE":
                monster.TalkIdle = value;
                break;

            case "THROWING":
                monster.Throwing = ParseNullableInt(monster, line, result);
                break;

            case "KEEPDISTANCE":
                monster.KeepDistance = ParseNullableInt(monster, line, result);
                break;

            case "CROSSBOW":
                monster.Crossbow = ParseNullableInt(monster, line, result);
                break;


            case "MAGICDEFENSE":
                monster.MagicDefense = ParseNullableInt(monster, line, result);
                break;

            case "MELEEDEFENSE":
                monster.MeleeDefense = ParseNullableInt(monster, line, result);
                break;

            case "MISSLEDEFENSE":
                monster.MissleDefense = ParseNullableInt(monster, line, result);
                break;

            case "SCAN":
                monster.Scan = ParseNullableInt(monster, line, result);
                break;

            case "WEAPON":
                monster.Weapon = value;
                break;

            case "CATAGORY":
            case "CATEGORY":
                monster.Catagory = value;
                break;

            case "UNARMED":
                monster.Unarmed = ParseNullableInt(monster, line, result);
                break;

            case "FEARFACTOR":
                monster.FearFactor = ParseNullableDouble(monster, line, result);
                break;

            case "CHESTARMOR":
                monster.ChestArmor = value;
                break;

            case "HEADARMOR":
                monster.HeadArmor = value;
                break;

            case "LEGARMOR":
                monster.LegArmor = value;
                break;

            case "CASTSPELL":
                monster.CastSpell = value;
                break;

            case "CASTHEAL":
                monster.CastHeal = value;
                break;

            case "STEALTH":
                monster.Stealth = ParseNullableInt(monster, line, result);
                break;

            case "TRADETALKFAREWELL":
                monster.TradeTalkFarewell = value;
                break;

            case "TRADETALKSUCCESS":
                monster.TradeTalkSuccess = value;
                break;

            case "AXE":
                monster.Axe = ParseNullableInt(monster, line, result);
                break;

            case "MACE":
                monster.Mace = ParseNullableInt(monster, line, result);
                break;

            case "FLAIL":
                monster.Flail = ParseNullableInt(monster, line, result);
                break;

            case "SCYTHE":
                monster.Scythe = ParseNullableInt(monster, line, result);
                break;

            case "FASTPROCESS":
                monster.FastProcess = ParseNullableDouble(monster, line, result);
                break;

            case "ROAM":
                monster.Roam = ParseNullableInt(monster, line, result);
                break;

            case "DAMAGEFRAGMENTS":
                monster.DamageFragments = value;
                break;

            case "ROBPLAYER":
                monster.RobPlayer = ParseNullableInt(monster, line, result);
                break;

            case "CASTLIGHTNING":
                monster.CastLightning = ParseNullableBool(monster, line, result);
                break;

            case "STAFF":
                monster.Staff = ParseNullableInt(monster, line, result);
                break;

            case "GROWTHMONSTERCHANCE":
                monster.GrowthMonsterChance = ParseNullableInt(monster, line, result);
                break;

            case "GROWTHMONSTERTIMEOUT":
                monster.GrowthMonsterTimeout = ParseNullableInt(monster, line, result);
                break;

            case "SPAWNITEM":
                monster.SpawnItem = value;
                break;

            case "SPAWNITEMCHANCE":
                monster.SpawnItemChance = ParseNullableDouble(monster, line, result);
                break;

            case "SPAWNITEMTIMEOUT":
                monster.SpawnItemTimeout = ParseNullableInt(monster, line, result);
                break;

            case "SPEAR":
                monster.Spear = ParseNullableInt(monster, line, result);
                break;

            case "ITEMTRAIL":
                monster.ItemTrail = value;
                break;

            case "ROAMCHANCE":
                monster.RoamChance = ParseNullableInt(monster, line, result);
                break;

            case "GREETINGANIMATION":
                monster.GreetingAnimation = value;
                break;

            case "CHASEITEM":
                monster.ChaseItem = value;
                break;

            case "IDLETRANSFORMITEM":
                monster.IdleTransformItem = value;
                break;

            case "SWIM":
                monster.Swim = ParseNullableInt(monster, line, result);
                break;

            case "WARPMOVE":
                monster.WarpMove = ParseNullableInt(monster, line, result);
                break;




            default:
                monster.UnknownFields.Add(new UnknownField(
                    key: key,
                    value: value,
                    lineNumber: line.LineNumber,
                    originalText: line.OriginalText));
                break;

            


        }
    }
    private static bool TryMatchCompoundNumberedKey(
    string key,
    string prefix,
    out int firstIndex,
    out int secondIndex)
    {
        firstIndex = -1;
        secondIndex = -1;

        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string suffix = key[prefix.Length..];

        if (string.IsNullOrWhiteSpace(suffix))
            return false;

        if (suffix.StartsWith('-'))
        {
            string secondPart = suffix[1..];

            if (int.TryParse(secondPart, out secondIndex))
            {
                firstIndex = 0;
                return true;
            }

            return false;
        }

        string[] parts = suffix.Split('-', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            return int.TryParse(parts[0], out firstIndex);
        }

        if (parts.Length == 2)
        {
            return int.TryParse(parts[0], out firstIndex)
                && int.TryParse(parts[1], out secondIndex);
        }

        return false;
    }


    private static bool TryApplyNumberedMonsterField(
    MonsterDefinition monster,
    IniKeyValueLine line,
    MonsterIniReadResult result)
    {
        string key = line.Key.Trim();

        if (TryMatchNumberedKey(key, "TreasureQty", out _))
        {
            int? parsed = ParseNullableInt(monster, line, result);
            if (parsed is not null)
                monster.TreasureQuantities.Add(parsed.Value);

            return true;
        }

        if (TryMatchNumberedKey(key, "TreasureChance", out _))
        {
            double? parsed = ParseNullableDouble(monster, line, result);
            if (parsed is not null)
                monster.TreasureChances.Add(parsed.Value);

            return true;
        }

        if (TryMatchNumberedKey(key, "Treasure", out _))
        {
            monster.Treasures.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "FriendCatagory", out _) ||
            TryMatchNumberedKey(key, "FriendCategory", out _))
        {
            monster.FriendCatagories.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "EnemyCatagory", out _) ||
            TryMatchNumberedKey(key, "EnemyCategory", out _))
        {
            monster.EnemyCatagories.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "TradeGroup", out _))
        {
            monster.TradeGroups.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "Friend", out _))
        {
            monster.Friends.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "QuestTakeItem", out _))
        {
            monster.QuestTakeItems.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "QuestTalk", out _))
        {
            monster.QuestTalkEntries.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "QuestGiveItem", out _))
        {
            monster.QuestGiveItems.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "QuestGiveQTy", out _) ||
            TryMatchNumberedKey(key, "QuestGiveQty", out _))
        {
            monster.QuestGiveQuantities.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "QuestGiveXP", out _))
        {
            monster.QuestGiveExperience.Add(line.Value);
            return true;
        }

        if (TryMatchCompoundNumberedKey(key, "QuestGiveData1", out _, out _))
        {
            monster.QuestGiveData1.Add(line.Value);
            return true;
        }

        if (TryMatchCompoundNumberedKey(key, "QuestGiveData2", out _, out _))
        {
            monster.QuestGiveData2.Add(line.Value);
            return true;
        }

        if (TryMatchCompoundNumberedKey(key, "QuestGiveData3", out _, out _))
        {
            monster.QuestGiveData3.Add(line.Value);
            return true;
        }

        if (TryMatchCompoundNumberedKey(key, "QuestGiveData4", out _, out _))
        {
            monster.QuestGiveData4.Add(line.Value);
            return true;
        }

        if (TryMatchCompoundNumberedKey(key, "TreasureData1", out _, out _))
        {
            monster.TreasureData1.Add(line.Value);
            return true;
        }

        if (TryMatchCompoundNumberedKey(key, "TreasureData2", out _, out _))
        {
            monster.TreasureData2.Add(line.Value);
            return true;
        }

        if (TryMatchCompoundNumberedKey(key, "TreasureData3", out _, out _))
        {
            monster.TreasureData3.Add(line.Value);
            return true;
        }

        if (TryMatchCompoundNumberedKey(key, "TreasureData4", out _, out _))
        {
            monster.TreasureData4.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "QuestTakeQty", out _) ||
            TryMatchNumberedKey(key, "QuestTakeQTy", out _))
        {
            monster.QuestTakeQuantities.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "QuestGiveTame", out _))
        {
            monster.QuestGiveTames.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "TradeGroupSellMax", out _))
        {
            monster.TradeGroupSellMaximums.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "TreasureTotalUses", out _))
        {
            monster.TreasureTotalUses.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "TreasureText", out _))
        {
            monster.TreasureTexts.Add(line.Value);
            return true;
        }

        return false;
    }

    private static bool TryMatchNumberedKey(
        string key,
        string prefix,
        out int index)
    {
        index = -1;

        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string suffix = key[prefix.Length..];

        return int.TryParse(suffix, out index);
    }

    private static int? ParseNullableInt(
        MonsterDefinition monster,
        IniKeyValueLine line,
        MonsterIniReadResult result)
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

        monster.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static double? ParseNullableDouble(
        MonsterDefinition monster,
        IniKeyValueLine line,
        MonsterIniReadResult result)
    {
        string value = line.Value.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsed))
        {
            return parsed;
        }

        var issue = new DefinitionParseIssue(
            message: "Expected decimal number value.",
            lineNumber: line.LineNumber,
            key: line.Key,
            value: line.Value);

        monster.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool? ParseNullableBool(
        MonsterDefinition monster,
        IniKeyValueLine line,
        MonsterIniReadResult result)
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

        monster.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool TryParseInt(string value, out int parsed)
    {
        return int.TryParse(value.Trim(), out parsed);
    }
}