using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Reads world.ini as one global configuration document.
/// Known terms are based on the active world.ini sample plus strings found in server2.exe.
/// Unknown settings are preserved instead of rejected.
/// </summary>
public static class WorldIniReader
{
    private static readonly HashSet<string> KnownFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "SuperAdmin",
        "UltraAdmin",
        "BuilderAdmin",
        "SecretClient",
        "BanClient",
        "AutoShutdown",
        "AdminSerial",
        "MasterPassword",

        "MapSize",
        "WorldDepth",
        "StartXpos",
        "StartYpos",
        "StartZpos",
        "StartPlace",
        "DefaultSurface",
        "MaxElevation",
        "ClimbLowElevation",
        "ClimbHighElevation",
        "TerrainClimb",
        "Terrain",

        "SurfaceGrowth",
        "AllowSurfaceGrowth",
        "DesertSurface",
        "DesertSurfaceDamage",
        "SurfaceDamage",
        "PlayerSurfaceCost",

        "OwnerEmail",
        "EmailMessage",
        "ServerMessage",
        "ServerPort",
        "PostDelay",
        "GlobalChatDelay",
        "PPSLimit",
        "PPSLimitNote",
        "LogThreshold",
        "LogonTimeLimit",
        "PlayerExitDelay",
        "IdlePlayerTimeout",
        "MaxAllowedClients",
        "AllowDupIPClients",
        "MaxPlayerPerClient",
        "MaxDupIP",

        "SkillPoints",
        "AttributePoints",
        "SkillRollType",
        "SkillRollAlpha",
        "SkillCap",
        "SlowDownSkillPoints",
        "LevelXpLimit",

        "PKprotectionLevel",
        "PKLevelRange",
        "VitaePenalty",
        "VitaeFactor",
        "DeathProtectionTime",
        "TimeToResurrect",
        "FoodOnDeath",
        "MaxPlayerFood",
        "MaxPlayerWater",
        "PassedOutImage",
        "PlayerLives",

        "XPFactor",
        "XPFactorCombat",
        "XPFactorPKCombat",
        "XPFactorTrade",
        "DamagePercent",
        "PKDamagePercent",
        "PKDamageMinLife",

        "MobEffect",
        "MonsterSpread",
        "MonsterChase",
        "MonsterProcessSeconds",
        "MonsterSpawnProcessSeconds",
        "MonsterSpawnCount",
        "DefaultMonsterSpeed",
        "MaxPoison",
        "ZombiePoison",
        "MaxReproduceCount",

        "AttackStealthBonus",
        "AttackElevationBonus",
        "MeleeDefenseFactor",
        "MissleDefenseFactor",

        "ItemStackLimit",
        "ItemMapStackLimit",
        "ItemOwnerDecay",
        "MaxItemSkillBonus",
        "ItemSkillBonusValue",
        "RepairDegrade",
        "JewelryDecay",
        "CarryImage",

        "SpeedUpGrowth",
        "NoSeasons",

        "OpenReservedLand",
        "UsableUnClaimedLand",
        "MaxUnClaimCount",
        "MaxLandOwn",
        "LandClaimCost",
        "LandClaimMaxRange",
        "LandOwnerTimeToLiveDays",

        "GuildCreateCost",
        "GuildCreateLevel",
        "GuildJoinLevel",
        "GuildJoinCost",
        "GuildMaintainCost",
        "GuildXPPercent",
        "GuildLandClaimCost",
        "GuildNPCBuy",

        "MeteoriteRate",
        "MeteoriteSize",
        "MeteoriteSpawn",
        "MeteoriteItem",

        "MiningBraceSurface",
        "MiningDepthMax",
        "MiningDepthFactor",
        "MiningHazard",

        "ExplodeDebris",
        "ExplodeAnimation",
        "ExplodeReset",
        "CaveInAnimation",

        "MaxTameCount",
        "TameFactor",
        "TameLevelRange",
        "TameShareXPPercent",

        "NPCTraderStartGold",
        "NPCTraderItemTTL",
        "NPCIdleTimeout",
        "TraderBuyMax",
        "TraderBuyCost",
        "MuleBuyMax",
        "MuleBuyCost",

        "StarterItem",
        "StarterQty",
        "StarterSpell",

        "AutoStalkerId",
        "AutoStalkerLevel",

        "CTFRedImage",
        "CTFBlueImage",
        "CTFRedResurrect",
        "CTFBlueResurrect",

        "TeamFlag",
        "TeamName",
        "FactionTroop",

        "Content",
        "LootTimeToLive",
        "EconomyFactor",
        "SpamCount",
        "AppealTimeToLive",
        "DeletePlayerVDayDelay",
        "EventStart",
        "GunShotHit",

        "DarkSectorXpos",
        "DarkSectorYpos",
        "LightSectorXpos",
        "LightSectorYpos",

        "Swear",
        "TribunalMode"
    };

    private static readonly HashSet<string> KnownFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "PopupMotd",
        "DisableMail",
        "RestrictGlobal",
        "LogHistory",
        "NetLog",
        "Perks",
        "PerksLoginOnly",
        "GlobalRequiresPerks",
        "AllowImageChange",
        "AllowTopTen",
        "DisableClientSecurity",
        "DisableOptimizedMonsterMoves",
        "Bitch",
        "CaveInItemDestroy",
        "NoSeasons",
        "DisableNewbieIsland",
        "OpenReservedLand",
        "UsableUnClaimedLand",
        "FactionRules",
        "TraderDeath",
        "SaveWorldChanges",
        "WeaponCalcSkillReq",
        "DisableAutoStalkers",
        "NoPKPenalty",
    };

    public static WorldIniReadResult ReadFile(string path)
    {
        IniParseResult parseResult = IniParser.ParseFile(path);
        return ReadDocument(parseResult.Document);
    }

    public static WorldIniReadResult ReadDocument(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new WorldIniReadResult(document);

        foreach (IniLine line in document.Lines)
        {
            switch (line)
            {
                case IniKeyValueLine keyValueLine:
                    HandleKeyValueLine(keyValueLine, result);
                    break;

                case IniFlagLine flagLine:
                    HandleFlagLine(flagLine, result);
                    break;
            }
        }

        return result;
    }

    public static bool IsKnownField(string key)
    {
        return KnownFields.Contains(key);
    }

    public static bool IsKnownFlag(string flag)
    {
        return KnownFlags.Contains(flag);
    }

    private static void HandleKeyValueLine(
        IniKeyValueLine line,
        WorldIniReadResult result)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        bool isKnown = IsKnownField(key);

        result.World.Settings.Add(new WorldSetting
        {
            Key = key,
            Value = value,
            LineNumber = line.LineNumber,
            OriginalText = line.OriginalText,
            IsKnown = isKnown
        });

        if (!isKnown)
        {
            result.World.UnknownFields.Add(new UnknownField(
                key: key,
                value: value,
                lineNumber: line.LineNumber,
                originalText: line.OriginalText));
        }
    }

    private static void HandleFlagLine(
        IniFlagLine line,
        WorldIniReadResult result)
    {
        string flagName = line.Name.Trim();

        if (string.IsNullOrWhiteSpace(flagName))
            return;

        result.World.Flags.Add(flagName);

        if (!IsKnownFlag(flagName))
            result.World.UnknownFlags.Add(flagName);
    }
}