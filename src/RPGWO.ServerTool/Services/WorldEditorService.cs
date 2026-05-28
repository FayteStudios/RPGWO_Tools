using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Loads and saves world.ini rows for the first GUI editor.
/// </summary>
public sealed class WorldEditorService
{
    private readonly SaveOperationService _saveOperationService = new();

    public List<WorldSettingRow> LoadRows(string worldIniPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worldIniPath);

        WorldIniReadResult result = WorldIniReader.ReadFile(worldIniPath);

        Dictionary<string, WorldSettingRow> existing = new(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in result.World.Settings)
        {
            if (!existing.ContainsKey(setting.Key))
            {
                existing[setting.Key] = new WorldSettingRow
                {
                    Type = "Setting",
                    Key = setting.Key,
                    Value = setting.Value,
                    Enabled = true,
                    LineNumber = setting.LineNumber,
                    IsKnown = setting.IsKnown,
                    OriginalText = setting.OriginalText
                };
            }
        }

        foreach (string flag in result.World.Flags)
        {
            if (!existing.ContainsKey(flag))
            {
                existing[flag] = new WorldSettingRow
                {
                    Type = "Flag",
                    Key = flag,
                    Value = "",
                    Enabled = true,
                    LineNumber = 0,
                    IsKnown = WorldIniReader.IsKnownFlag(flag),
                    OriginalText = flag
                };
            }
        }

        var rows = new List<WorldSettingRow>();

        foreach (WorldSettingDef def in KnownWorldSettingDefs)
        {
            if (existing.TryGetValue(def.Key, out WorldSettingRow? existingRow))
            {
                existingRow.Type = def.Type;
                existingRow.IsKnown = true;
                rows.Add(existingRow);
            }
            else
            {
                rows.Add(new WorldSettingRow
                {
                    Type = def.Type,
                    Key = def.Key,
                    Value = "",
                    Enabled = false,
                    LineNumber = 0,
                    IsKnown = true,
                    OriginalText = ""
                });
            }
        }

        foreach (WorldSettingRow extra in existing.Values
            .Where(row => !KnownWorldSettingDefs.Any(def => def.Key.Equals(row.Key, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(row => row.Key))
        {
            rows.Add(extra);
        }

        return rows;
    }
    private sealed record WorldSettingDef(string Key, string Type);

    private static readonly WorldSettingDef[] KnownWorldSettingDefs =
    {
    new("OwnerEmail", "Setting"),
    new("ServerMessage", "Setting"),
    new("LogThreshold", "Setting"),
    new("GlobalChatDelay", "Setting"),
    new("PostDelay", "Setting"),
    new("ServerPost", "Setting"),
    new("PPSLimit", "Setting"),
    new("AllowDupIPClient", "Flag"),
    new("MaxDupIP", "Setting"),
    new("XPFactorCombat", "Setting"),
    new("XPFactorTrade", "Setting"),
    new("UltraAdmin", "Flag"),
    new("SecretClient", "Flag"),
    new("PopupMOTD", "Setting"),
    new("LogHistory", "Flag"),
    new("Perks", "Flag"),
    new("AllowImageChange", "Flag"),
    new("AllowTopTen", "Flag"),
    new("DisableClientSecurity", "Flag"),
    new("DisableOptimizedMonsterMoves", "Flag"),
    new("Bitch", "Flag"),
    new("NoSeasons", "Flag"),
    new("DisableNewbieIsland", "Flag"),
    new("OpenReservedLand", "Flag"),
    new("UseableUnclaimedLand", "Flag"),
    new("CaveInItemDestroy", "Flag"),

    new("MapSize", "Setting"),
    new("WorldDepth", "Setting"),
    new("StartingXPos", "Setting"),
    new("StartingYPos", "Setting"),
    new("StartingZPos", "Setting"),
    new("ClimbLow", "Setting"),
    new("ClimbHigh", "Setting"),
    new("SurfaceGrowth", "Setting"),
    new("SurfaceDamage", "Setting"),
    new("MaxUnclaimCount", "Setting"),
    new("MaxLandOwn", "Setting"),
    new("LandClaimCost", "Setting"),
    new("LandOwnerTimeToLiveDays", "Setting"),
    new("PlayerSurfaceCost", "Setting"),

    new("SkillPoints", "Setting"),
    new("AttributePoints", "Setting"),
    new("SkillRollType", "Setting"),
    new("SkillRollAlpha", "Setting"),
    new("PKProtectionLevel", "Setting"),
    new("PKLevelRange", "Setting"),
    new("MaxPlayerFood", "Setting"),
    new("MaxPlayerWater", "Setting"),
    new("FoodOnDeath", "Setting"),
    new("PassedOutImage", "Setting"),
    new("MaxPlayerPerAccount", "Setting"),
    new("VitaePenalty", "Setting"),
    new("MaxPoison", "Setting"),
    new("TimeToResurrect", "Setting"),
    new("DeathProtectionTime", "Setting"),

    new("AutoStalkerID", "Setting"),
    new("MobEffect", "Setting"),
    new("MonsterSpread", "Setting"),
    new("MonsterChase", "Setting"),
    new("MonsterProcessSeconds", "Setting"),
    new("MonsterSpawnCount", "Setting"),
    new("DefaultMonsterSpeed", "Setting"),
    new("MaxTameCount", "Setting"),
    new("NPCTraderStartGold", "Setting"),
    new("TraderBuyMax", "Setting"),
    new("TraderBuyCost", "Setting"),
    new("NPCIdleTimeout", "Setting"),
    new("TameShareXPPercent", "Setting"),

    new("ItemMapStackLimit", "Setting"),
    new("ItemOwnerDecay", "Setting"),
    new("MaxItemSkillBonus", "Setting"),
    new("MeteoriteItem", "Setting"),
    new("MiningBraceSurface", "Setting"),
    new("MiningDepthMax", "Setting"),
    new("MiningDepthFactor", "Setting"),

    new("GuildCreateCost", "Setting"),
    new("GuildCreateLevel", "Setting"),
    new("GuildMaintainCost", "Setting"),
    new("GuildLandClaimCost", "Setting"),

    new("CTFRedImage", "Setting"),
    new("CTFBlueImage", "Setting"),
    new("CTFRedResurrect", "Setting"),
    new("CTFBlueResurrect", "Setting")
};

    public SaveValidationResult SaveRows(
        string worldIniPath,
        IReadOnlyList<WorldSettingRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worldIniPath);
        ArgumentNullException.ThrowIfNull(rows);

        return _saveOperationService.SaveWithBackup(
            worldIniPath,
            saveAction: () =>
            {
                IniParseResult parseResult = IniParser.ParseFile(worldIniPath);

                ApplyRowsToDocument(parseResult.Document, rows);

                IniWriter.WriteFile(worldIniPath, parseResult.Document);
            },
            validateAction: () =>
            {
                WorldIniReadResult result = WorldIniReader.ReadFile(worldIniPath);

                return (
                    UnknownCount: result.UnknownFieldCount + result.UnknownFlagCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static void ApplyRowsToDocument(
        IniDocument document,
        IReadOnlyList<WorldSettingRow> rows)
    {
        // Simple first version:
        // Rebuild all active setting/flag lines from the editable rows.
        // Comments and blank lines from the original document are preserved at the top
        // only if they were comments/blanks before active content.
        //
        // Later we can do line-exact editing, but this gets us a usable world.ini editor now.

        var preservedHeaderLines = new List<IniLine>();

        foreach (IniLine line in document.Lines)
        {
            if (line is IniCommentLine or IniBlankLine)
            {
                preservedHeaderLines.Add(line);
                continue;
            }

            break;
        }

        document.Lines.Clear();

        foreach (IniLine line in preservedHeaderLines)
            document.Lines.Add(line);

        foreach (WorldSettingRow row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Key))
                continue;

            if (row.IsFlag)
            {
                if (!row.Enabled)
                    continue;

                document.Lines.Add(new IniFlagLine
                {
                    LineNumber = 0,
                    OriginalText = row.Key,
                    Name = row.Key
                });

                continue;
            }

            document.Lines.Add(new IniKeyValueLine
            {
                LineNumber = 0,
                OriginalText = $"{row.Key}={row.Value}",
                Key = row.Key,
                Value = row.Value ?? "",
                Separator = "=",
                IsModified = false
            });
        }
    }
}