using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Loads and saves treasure.ini data for the GUI treasure editor.
/// This reads editable rows directly from the INI document so repeated lines are preserved as rows.
/// </summary>
public sealed class TreasureEditorService
{
    private readonly SaveOperationService _saveOperationService = new();

    public List<TreasureEntryRow> LoadEntryRows(string treasureIniPath, int treasureId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(treasureIniPath);

        IniParseResult parseResult = IniParser.ParseFile(treasureIniPath);

        TreasureBlock? block = FindTreasureBlock(parseResult.Document, treasureId);

        if (block is null)
            return new List<TreasureEntryRow>();

        var entries = new List<Dictionary<string, string>>();
        Dictionary<string, string>? current = null;

        void Flush()
        {
            if (current is null || current.Count == 0)
                return;

            entries.Add(current);
            current = null;
        }

        for (int index = block.Value.StartIndex + 1; index < block.Value.EndIndexExclusive; index++)
        {
            if (parseResult.Document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            string key = keyValueLine.Key.Trim();
            string value = keyValueLine.Value.Trim();

            if (key.Equals("Item", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("SpellID", StringComparison.OrdinalIgnoreCase))
            {
                Flush();

                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [key] = value
                };

                continue;
            }

            if (current is null)
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            current[key] = value;
        }

        Flush();

        var rows = new List<TreasureEntryRow>();

        for (int index = 0; index < entries.Count; index++)
        {
            Dictionary<string, string> entry = entries[index];

            rows.Add(new TreasureEntryRow
            {
                EntryIndex = index + 1,
                Item = entry.TryGetValue("Item", out string? item) ? item : "",
                SkillId = entry.TryGetValue("SkillId", out string? skillId) ? skillId : "",
                SkillLow = entry.TryGetValue("SkillLow", out string? skillLow) ? skillLow : "",
                SkillHigh = entry.TryGetValue("SkillHigh", out string? skillHigh) ? skillHigh : "",
                Chance = entry.TryGetValue("Chance", out string? chance) ? chance : "",
                SpellID = entry.TryGetValue("SpellID", out string? spellId) ? spellId : "",
                SpellData = entry.TryGetValue("SpellData", out string? spellData) ? spellData : "",
                FieldCount = entry.Count
            });
        }

        return rows;
    }
    public List<TreasureFieldRow> LoadFieldRows(
        string treasureIniPath,
        int treasureId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(treasureIniPath);

        IniParseResult parseResult = IniParser.ParseFile(treasureIniPath);

        TreasureBlock? block = FindTreasureBlock(parseResult.Document, treasureId);

        if (block is null)
            return new List<TreasureFieldRow>();

        var rows = new List<TreasureFieldRow>();

        for (int index = block.Value.StartIndex + 1; index < block.Value.EndIndexExclusive; index++)
        {
            if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
            {
                rows.Add(new TreasureFieldRow
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
                rows.Add(new TreasureFieldRow
                {
                    Type = "Flag",
                    Key = flagLine.Name,
                    Value = "",
                    Enabled = true,
                    IsKnown = false
                });
            }
        }

        return rows;
    }

    public SaveValidationResult SaveTreasureRows(
        string treasureIniPath,
        int treasureId,
        IReadOnlyList<TreasureFieldRow> editedRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(treasureIniPath);
        ArgumentNullException.ThrowIfNull(editedRows);

        return _saveOperationService.SaveWithBackup(
            treasureIniPath,
            saveAction: () =>
            {
                IniParseResult parseResult = IniParser.ParseFile(treasureIniPath);

                ApplyTreasureRowsToDocument(
                    parseResult.Document,
                    treasureId,
                    editedRows);

                IniWriter.WriteFile(treasureIniPath, parseResult.Document);
            },
            validateAction: () =>
            {
                TreasureIniReadResult result = TreasureIniReader.ReadFile(treasureIniPath);

                return (
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static void ApplyTreasureRowsToDocument(
        IniDocument document,
        int treasureId,
        IReadOnlyList<TreasureFieldRow> editedRows)
    {
        TreasureBlock? block = FindTreasureBlock(document, treasureId);

        if (block is null)
            throw new InvalidOperationException($"Could not find Treasure block with internal ID={treasureId}.");

        for (int index = block.Value.EndIndexExclusive - 1; index > block.Value.StartIndex; index--)
            document.Lines.RemoveAt(index);

        int insertIndex = block.Value.StartIndex + 1;

        foreach (TreasureFieldRow row in editedRows)
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
            || key.Equals("Item", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SkillId", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SkillLow", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SkillHigh", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SpellID", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SpellData", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Cost", StringComparison.OrdinalIgnoreCase);
    }

    private static List<TreasureBlock> FindTreasureBlocks(IniDocument document)
    {
        var blocks = new List<TreasureBlock>();
        int fallbackId = 0;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsTreasureMarker(keyValueLine))
                continue;

            int treasureId;

            if (!int.TryParse(keyValueLine.Value.Trim(), out treasureId))
                treasureId = ++fallbackId;

            int endIndexExclusive = document.Lines.Count;

            for (int nextIndex = index + 1; nextIndex < document.Lines.Count; nextIndex++)
            {
                if (document.Lines[nextIndex] is not IniKeyValueLine nextKeyValueLine)
                    continue;

                if (IsTreasureMarker(nextKeyValueLine))
                {
                    endIndexExclusive = nextIndex;
                    break;
                }
            }

            blocks.Add(new TreasureBlock(
                TreasureId: treasureId,
                MarkerValue: keyValueLine.Value,
                StartIndex: index,
                EndIndexExclusive: endIndexExclusive));
        }

        return blocks;
    }

    private static TreasureBlock? FindTreasureBlock(
        IniDocument document,
        int treasureId)
    {
        return FindTreasureBlocks(document)
            .FirstOrDefault(block => block.TreasureId == treasureId);
    }
    public List<TreasureRow> LoadTreasureRows(string treasureIniPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(treasureIniPath);

        IniParseResult parseResult = IniParser.ParseFile(treasureIniPath);

        var rows = new List<TreasureRow>();
        int id = 1;

        for (int index = 0; index < parseResult.Document.Lines.Count; index++)
        {
            if (parseResult.Document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!keyValueLine.Key.Equals("Treasure", StringComparison.OrdinalIgnoreCase))
                continue;

            int itemCount = 0;
            int issueCount = 0;

            int nextIndex = index + 1;

            while (nextIndex < parseResult.Document.Lines.Count)
            {
                if (parseResult.Document.Lines[nextIndex] is IniKeyValueLine nextKeyValue &&
                    nextKeyValue.Key.Equals("Treasure", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (parseResult.Document.Lines[nextIndex] is IniKeyValueLine entryLine)
                {
                    if (entryLine.Key.Equals("Item", StringComparison.OrdinalIgnoreCase) ||
                        entryLine.Key.Equals("SpellID", StringComparison.OrdinalIgnoreCase))
                    {
                        itemCount++;
                    }

                    if (!IsKnownField(entryLine.Key) &&
                        !entryLine.Key.Equals("Treasure", StringComparison.OrdinalIgnoreCase))
                    {
                        issueCount++;
                    }
                }

                nextIndex++;
            }

            rows.Add(new TreasureRow
            {
                Id = id,
                Name = keyValueLine.Value,
                ItemCount = itemCount,
                IssueCount = issueCount
            });

            id++;
        }

        return rows;
    }
    private static bool IsTreasureMarker(IniKeyValueLine line)
    {
        return line.Key.Equals("Treasure", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("TreasureID", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct TreasureBlock(
        int TreasureId,
        string MarkerValue,
        int StartIndex,
        int EndIndexExclusive);
}