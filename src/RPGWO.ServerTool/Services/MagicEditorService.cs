using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Loads and saves magic.ini data for the GUI magic editor.
/// This reads editable rows directly from the INI document so repeated rune/animation/effect lines are preserved.
/// </summary>
public sealed class MagicEditorService
{
    private readonly SaveOperationService _saveOperationService = new();
    private static readonly MagicFieldDef[] MagicFieldDefs =
    {
        new("Name", "Name", "General"),
        new("Description", "Description", "General"),
        new("Skill", "Skill", "General"),
        new("SkillToLearn", "SkillToLearn", "General"),
        new("SkillMin", "SkillMin", "General"),
        new("SkillMax", "SkillMax", "General"),
        new("ManaCost", "ManaCost", "General"),
        new("Animation", "Animation", "General"),

        new("WandUse", "WandUse", "Stats"),
        new("Range", "Range", "Stats"),
        new("Target", "Target", "Stats", new[] { "", "Self", "Other", "Spot", "Ward", "Item" }),
        new("CastTime", "CastTime", "Stats"),
        new("SuccessXP", "SuccessXP", "Stats"),
        new("FailedXP", "FailedXP", "Stats"),

        new("Rune1", "Rune1", "Runes"),
        new("Rune2", "Rune2", "Runes"),
        new("Rune3", "Rune3", "Runes"),
        new("Rune4", "Rune4", "Runes"),
        new("Rune5", "Rune5", "Runes"),

        new("Variance", "Variance", "Effects"),
        new("Life", "Life", "Effects"),
        new("LifeRenewal", "LifeRenewal", "Effects"),
        new("LifeSteal", "LifeSteal", "Effects"),
        new("Stamina", "Stamina", "Effects"),
        new("StaminaRenewal", "StaminaRenewal", "Effects"),
        new("StaminaSteal", "StaminaSteal", "Effects"),
        new("Mana", "Mana", "Effects"),
        new("ManaRenewal", "ManaRenewal", "Effects"),
        new("ManaSteal", "ManaSteal", "Effects"),
        new("Cure", "Cure", "Effects"),
        new("Ice", "Ice", "Effects"),
        new("Blind", "Blind", "Effects"),
        new("Hero", "Hero", "Effects"),
        new("Strength", "Strength", "Effects"),
        new("Dexterity", "Dexterity", "Effects"),
        new("Quickness", "Quickness", "Effects"),
        new("Intelligence", "Intelligence", "Effects"),
        new("Wisdom", "Wisdom", "Effects"),
        new("Armor", "Armor", "Effects"),
        new("Improve", "Improve", "Effects"),
        new("EssenceSteal", "EssenceSteal", "Effects"),
        new("DamageType", "DamageType", "Effects", new[] { "", "Cut", "Bash", "Thrust", "Fire", "Cold", "Electric", "Cut/Thrust", "Magic" }),
    };

    private sealed record MagicFieldDef(
        string Label,
        string IniKey,
        string Group,
        string[]? Options = null);
    public List<MagicRow> LoadMagicRows(string magicIniPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(magicIniPath);

        IniParseResult parseResult = IniParser.ParseFile(magicIniPath);
        MagicIniReadResult readResult = MagicIniReader.ReadFile(magicIniPath);

        var rows = new List<MagicRow>();

        foreach (MagicBlock block in FindMagicBlocks(parseResult.Document))
        {
            string name = "";
            string skillName = "";
            int runeCount = 0;
            int fieldCount = 0;
            int flagCount = 0;

            for (int index = block.StartIndex + 1; index < block.EndIndexExclusive; index++)
            {
                if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
                {
                    fieldCount++;

                    if (keyValueLine.Key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        name = keyValueLine.Value;

                    if (keyValueLine.Key.Equals("Skill", StringComparison.OrdinalIgnoreCase))
                        skillName = keyValueLine.Value;

                    if (keyValueLine.Key.StartsWith("Rune", StringComparison.OrdinalIgnoreCase))
                        runeCount++;

                    continue;
                }

                if (parseResult.Document.Lines[index] is IniFlagLine)
                    flagCount++;
            }

            var spell = readResult.Spells.FirstOrDefault(spell => spell.Id == block.SpellId);

            rows.Add(new MagicRow
            {
                Id = block.SpellId,
                Name = !string.IsNullOrWhiteSpace(name)
                    ? name
                    : spell?.Name ?? "",
                Skill = !string.IsNullOrWhiteSpace(skillName)
                    ? skillName
                    : spell?.Skill ?? "",
                RuneCount = runeCount,
                FieldCount = fieldCount,
                FlagCount = flagCount,
                UnknownCount = spell?.UnknownFields.Count ?? 0,
                IssueCount = spell?.Issues.Count ?? 0
            });
        }

        return rows
            .OrderBy(row => row.Id)
            .ToList();
    }
    public List<MagicFieldRow> CreateBlankFieldRows(int spellId)
    {
        return BuildFieldRows(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }
    private static List<MagicFieldRow> BuildFieldRows(Dictionary<string, string> values)
    {
        var rows = new List<MagicFieldRow>();

        foreach (MagicFieldDef field in MagicFieldDefs)
        {
            rows.Add(new MagicFieldRow
            {
                Label = field.Label,
                IniKey = field.IniKey,
                Group = field.Group,
                Value = values.TryGetValue(field.IniKey, out string? value) ? value : "",
                Enabled = values.ContainsKey(field.IniKey),
                Kind = "Text",
                Options = field.Options?.ToList() ?? new List<string>()
            });
        }

        return rows;
    }
    public List<MagicFieldRow> LoadFieldRows(string magicIniPath, int spellId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(magicIniPath);

        IniParseResult parseResult = IniParser.ParseFile(magicIniPath);

        MagicBlock? block = FindMagicBlock(parseResult.Document, spellId);

        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        if (block is not null)
        {
            for (int index = block.Value.StartIndex + 1; index < block.Value.EndIndexExclusive; index++)
            {
                if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
                    values[keyValueLine.Key] = keyValueLine.Value;
            }
        }

        return BuildFieldRows(values);
    }

    public SaveValidationResult SaveMagicRows(
        string magicIniPath,
        int spellId,
        IReadOnlyList<MagicFieldRow> editedRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(magicIniPath);
        ArgumentNullException.ThrowIfNull(editedRows);

        return _saveOperationService.SaveWithBackup(
            magicIniPath,
            saveAction: () =>
            {
                IniParseResult parseResult = IniParser.ParseFile(magicIniPath);

                ApplyMagicRowsToDocument(
                    parseResult.Document,
                    spellId,
                    editedRows);

                IniWriter.WriteFile(magicIniPath, parseResult.Document);
            },
            validateAction: () =>
            {
                MagicIniReadResult result = MagicIniReader.ReadFile(magicIniPath);

                return (
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static void ApplyMagicRowsToDocument(
        IniDocument document,
        int spellId,
        IReadOnlyList<MagicFieldRow> editedRows)
    {
        MagicBlock? block = FindMagicBlock(document, spellId);

        int insertIndex;

        if (block is null)
        {
            if (document.Lines.Count > 0)
                document.Lines.Add(new IniBlankLine { LineNumber = 0, OriginalText = "" });

            document.Lines.Add(new IniKeyValueLine
            {
                LineNumber = 0,
                OriginalText = $"Spell={spellId}",
                Key = "Spell",
                Value = spellId.ToString(),
                Separator = "=",
                IsModified = false
            });

            insertIndex = document.Lines.Count;
        }
        else
        {
            for (int index = block.Value.EndIndexExclusive - 1; index > block.Value.StartIndex; index--)
                document.Lines.RemoveAt(index);

            insertIndex = block.Value.StartIndex + 1;
        }

        foreach (MagicFieldRow row in editedRows)
        {
            if (string.IsNullOrWhiteSpace(row.IniKey))
                continue;

            string value = row.Value?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(value))
                continue;

            document.Lines.Insert(
                insertIndex,
                new IniKeyValueLine
                {
                    LineNumber = 0,
                    OriginalText = $"{row.IniKey}={value}",
                    Key = row.IniKey,
                    Value = value,
                    Separator = "=",
                    IsModified = false
                });

            insertIndex++;
        }
    }
    public SaveValidationResult DeleteSpell(string magicIniPath, int spellId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(magicIniPath);

        return _saveOperationService.SaveWithBackup(
            magicIniPath,
            saveAction: () =>
            {
                IniParseResult parseResult = IniParser.ParseFile(magicIniPath);

                MagicBlock? block = FindMagicBlock(parseResult.Document, spellId);

                if (block is null)
                    throw new InvalidOperationException($"Could not find Spell={spellId}.");

                for (int index = block.Value.EndIndexExclusive - 1; index >= block.Value.StartIndex; index--)
                    parseResult.Document.Lines.RemoveAt(index);

                IniWriter.WriteFile(magicIniPath, parseResult.Document);
            },
            validateAction: () =>
            {
                MagicIniReadResult result = MagicIniReader.ReadFile(magicIniPath);

                return (
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }
    private static List<MagicBlock> FindMagicBlocks(IniDocument document)
    {
        var blocks = new List<MagicBlock>();

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsMagicMarker(keyValueLine))
                continue;

            if (!int.TryParse(keyValueLine.Value.Trim(), out int spellId))
                continue;

            int endIndexExclusive = document.Lines.Count;

            for (int nextIndex = index + 1; nextIndex < document.Lines.Count; nextIndex++)
            {
                if (document.Lines[nextIndex] is not IniKeyValueLine nextKeyValueLine)
                    continue;

                if (IsMagicMarker(nextKeyValueLine))
                {
                    endIndexExclusive = nextIndex;
                    break;
                }
            }

            blocks.Add(new MagicBlock(
                SpellId: spellId,
                StartIndex: index,
                EndIndexExclusive: endIndexExclusive));
        }

        return blocks;
    }

    private static MagicBlock? FindMagicBlock(
        IniDocument document,
        int spellId)
    {
        return FindMagicBlocks(document)
            .FirstOrDefault(block => block.SpellId == spellId);
    }

    private static bool IsMagicMarker(IniKeyValueLine line)
    {
        return line.Key.Equals("Spell", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("SpellID", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("Magic", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct MagicBlock(
        int SpellId,
        int StartIndex,
        int EndIndexExclusive);
}