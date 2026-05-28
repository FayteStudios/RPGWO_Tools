using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Loads and saves monster.ini data for the GUI monster editor.
/// This reads editable rows directly from the INI document so repeated treasure/trade/quest lines are preserved.
/// </summary>
public sealed class MonsterEditorService
{
    private readonly SaveOperationService _saveOperationService = new();

    public List<MonsterRow> LoadMonsterRows(string monsterIniPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(monsterIniPath);

        IniParseResult parseResult = IniParser.ParseFile(monsterIniPath);
        MonsterIniReadResult readResult = MonsterIniReader.ReadFile(monsterIniPath);

        var rows = new List<MonsterRow>();

        foreach (MonsterBlock block in FindMonsterBlocks(parseResult.Document))
        {
            string name = "";
            int fieldCount = 0;
            int flagCount = 0;

            for (int index = block.StartIndex + 1; index < block.EndIndexExclusive; index++)
            {
                if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
                {
                    fieldCount++;

                    if (keyValueLine.Key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        name = keyValueLine.Value;

                    continue;
                }

                if (parseResult.Document.Lines[index] is IniFlagLine)
                    flagCount++;
            }

            var monster = readResult.Monsters.FirstOrDefault(monster => monster.Id == block.MonsterId);

            rows.Add(new MonsterRow
            {
                Id = block.MonsterId,
                Name = !string.IsNullOrWhiteSpace(name)
                    ? name
                    : monster?.Name ?? "",
                FieldCount = fieldCount,
                FlagCount = flagCount,
                UnknownCount = monster?.UnknownFields.Count ?? 0,
                IssueCount = monster?.Issues.Count ?? 0
            });
        }

        return rows
            .OrderBy(row => row.Id)
            .ToList();
    }

    public List<MonsterFieldRow> LoadFieldRows(
        string monsterIniPath,
        int monsterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(monsterIniPath);

        IniParseResult parseResult = IniParser.ParseFile(monsterIniPath);

        MonsterBlock? block = FindMonsterBlock(parseResult.Document, monsterId);

        if (block is null)
            return new List<MonsterFieldRow>();

        var rows = new List<MonsterFieldRow>();

        for (int index = block.Value.StartIndex + 1; index < block.Value.EndIndexExclusive; index++)
        {
            if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
            {
                rows.Add(new MonsterFieldRow
                {
                    Type = "Field",
                    Key = keyValueLine.Key,
                    Value = keyValueLine.Value,
                    Enabled = true,
                    IsKnown = IsKnownField(keyValueLine.Key)
                });

                continue;
            }

            if (parseResult.Document.Lines[index] is IniFlagLine flagLine)
            {
                rows.Add(new MonsterFieldRow
                {
                    Type = "Flag",
                    Key = flagLine.Name,
                    Value = "",
                    Enabled = true,
                    IsKnown = IsKnownFlag(flagLine.Name)
                });
            }
        }

        return rows;
    }

    public SaveValidationResult SaveMonsterRows(
        string monsterIniPath,
        int monsterId,
        IReadOnlyList<MonsterFieldRow> editedRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(monsterIniPath);
        ArgumentNullException.ThrowIfNull(editedRows);

        return _saveOperationService.SaveWithBackup(
            monsterIniPath,
            saveAction: () =>
            {
                IniParseResult parseResult = IniParser.ParseFile(monsterIniPath);

                ApplyMonsterRowsToDocument(
                    parseResult.Document,
                    monsterId,
                    editedRows);

                IniWriter.WriteFile(monsterIniPath, parseResult.Document);
            },
            validateAction: () =>
            {
                MonsterIniReadResult result = MonsterIniReader.ReadFile(monsterIniPath);

                return (
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static void ApplyMonsterRowsToDocument(
        IniDocument document,
        int monsterId,
        IReadOnlyList<MonsterFieldRow> editedRows)
    {
        MonsterBlock? block = FindMonsterBlock(document, monsterId);

        if (block is null)
            throw new InvalidOperationException($"Could not find Monster={monsterId}.");

        for (int index = block.Value.EndIndexExclusive - 1; index > block.Value.StartIndex; index--)
            document.Lines.RemoveAt(index);

        int insertIndex = block.Value.StartIndex + 1;

        foreach (MonsterFieldRow row in editedRows)
        {
            if (string.IsNullOrWhiteSpace(row.Key))
                continue;

            if (row.IsFlag)
            {
                if (!row.Enabled)
                    continue;

                document.Lines.Insert(
                    insertIndex,
                    new IniFlagLine
                    {
                        LineNumber = 0,
                        OriginalText = row.Key,
                        Name = row.Key
                    });

                insertIndex++;
                continue;
            }

            document.Lines.Insert(
                insertIndex,
                new IniKeyValueLine
                {
                    LineNumber = 0,
                    OriginalText = $"{row.Key}={row.Value}",
                    Key = row.Key,
                    Value = row.Value ?? "",
                    Separator = "=",
                    IsModified = false
                });

            insertIndex++;
        }
    }

    private static bool IsKnownField(string key)
    {
        return key.Equals("Name", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Image", StringComparison.OrdinalIgnoreCase)
            || key.Equals("ImageType", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Level", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Health", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Life", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Stamina", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Mana", StringComparison.OrdinalIgnoreCase)
            || key.Equals("MeleeDefense", StringComparison.OrdinalIgnoreCase)
            || key.Equals("MissleDefense", StringComparison.OrdinalIgnoreCase)
            || key.Equals("MagicDefense", StringComparison.OrdinalIgnoreCase)
            || key.Equals("MagicPower", StringComparison.OrdinalIgnoreCase)
            || key.Equals("magicpower", StringComparison.OrdinalIgnoreCase)
            || key.Equals("AttackSpeed", StringComparison.OrdinalIgnoreCase)
            || key.Equals("DamageLow", StringComparison.OrdinalIgnoreCase)
            || key.Equals("DamageHigh", StringComparison.OrdinalIgnoreCase)
            || key.Equals("DamageType", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Weapon", StringComparison.OrdinalIgnoreCase)
            || key.Equals("RangeWeapon", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Unarmed", StringComparison.OrdinalIgnoreCase)
            || key.Equals("ChestArmor", StringComparison.OrdinalIgnoreCase)
            || key.Equals("HeadArmor", StringComparison.OrdinalIgnoreCase)
            || key.Equals("LegArmor", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Shield", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Sheild", StringComparison.OrdinalIgnoreCase)
            || key.Equals("DeadItem", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Catagory", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Category", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Scan", StringComparison.OrdinalIgnoreCase)
            || key.Equals("FearFactor", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Fearfactor", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Speed", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Run", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Roam", StringComparison.OrdinalIgnoreCase)
            || key.Equals("KeepDistance", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Treasure", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("Treasure", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("EnemyCatagory", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("EnemyCategory", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("FriendCatagory", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("FriendCategory", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("Friend", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("Trade", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("Talk", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("Quest", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("Cast", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("cast", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Sword", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Dagger", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Axe", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Mace", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Flail", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Scythe", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Staff", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Bow", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Crossbow", StringComparison.OrdinalIgnoreCase)
            || key.Equals("crossbow", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Throwing", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Sneak", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Stealth", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Undead", StringComparison.OrdinalIgnoreCase)
            || key.Equals("GrowthMonsterChance", StringComparison.OrdinalIgnoreCase)
            || key.Equals("GrowthMonsterTimeout", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SpawnItem", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SpawnItemChance", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SpawnItemTimeout", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SpawnMonster", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SpawnMonsterChance", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SpawnMonsterTimeout", StringComparison.OrdinalIgnoreCase)
            || key.Equals("ItemTrail", StringComparison.OrdinalIgnoreCase)
            || key.Equals("ChaseItem", StringComparison.OrdinalIgnoreCase)
            || key.Equals("WarpMove", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKnownFlag(string flag)
    {
        return flag.Equals("HelpFriends", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("NotTamable", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("LogHistory", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("ScanAlot", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("NotAttackable", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("Desert", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("Unique", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("AttackHigh", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("AttackMid", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("AttackLow", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("MoveFast", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("ItemDamageImmune", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("AttackMonsters", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("StealthVision", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("TradeAlwaysStock", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("IgnorePlayers", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("StandStill", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("AirMove", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("NoHands", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("GhostMove", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("NeedWarmth", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("OnlyBuyLoot", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("WaterMove", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("EatGrass", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("IgnoreNewbies", StringComparison.OrdinalIgnoreCase);
    }

    private static List<MonsterBlock> FindMonsterBlocks(IniDocument document)
    {
        var blocks = new List<MonsterBlock>();

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsMonsterMarker(keyValueLine))
                continue;

            if (!int.TryParse(keyValueLine.Value.Trim(), out int monsterId))
                continue;

            int endIndexExclusive = document.Lines.Count;

            for (int nextIndex = index + 1; nextIndex < document.Lines.Count; nextIndex++)
            {
                if (document.Lines[nextIndex] is not IniKeyValueLine nextKeyValueLine)
                    continue;

                if (IsMonsterMarker(nextKeyValueLine))
                {
                    endIndexExclusive = nextIndex;
                    break;
                }
            }

            blocks.Add(new MonsterBlock(
                MonsterId: monsterId,
                StartIndex: index,
                EndIndexExclusive: endIndexExclusive));
        }

        return blocks;
    }

    private static MonsterBlock? FindMonsterBlock(
        IniDocument document,
        int monsterId)
    {
        return FindMonsterBlocks(document)
            .FirstOrDefault(block => block.MonsterId == monsterId);
    }

    private static bool IsMonsterMarker(IniKeyValueLine line)
    {
        return line.Key.Equals("Monster", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("MonsterID", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct MonsterBlock(
        int MonsterId,
        int StartIndex,
        int EndIndexExclusive);
}