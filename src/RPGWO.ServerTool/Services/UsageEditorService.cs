using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Loads and saves itemuse.ini data for the GUI usage editor.
/// itemuse.ini entries are separated by ItemTool= lines, not by ItemUse= markers.
/// </summary>
public sealed class UsageEditorService
{
    private readonly SaveOperationService _saveOperationService = new();

    public List<UsageRow> LoadUsageRows(string usageIniPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usageIniPath);

        IniParseResult parseResult = IniParser.ParseFile(usageIniPath);
        UsageIniReadResult readResult = UsageIniReader.ReadFile(usageIniPath);

        var rows = new List<UsageRow>();

        foreach (UsageBlock block in FindUsageBlocks(parseResult.Document))
        {
            string itemTool = "";
            string itemFocus = "";
            string skill = "";
            int fieldCount = 0;
            int flagCount = 0;

            for (int index = block.StartIndex; index < block.EndIndexExclusive; index++)
            {
                if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
                {
                    fieldCount++;

                    if (keyValueLine.Key.Equals("ItemTool", StringComparison.OrdinalIgnoreCase))
                        itemTool = keyValueLine.Value;

                    if (keyValueLine.Key.Equals("ItemFocus", StringComparison.OrdinalIgnoreCase))
                        itemFocus = keyValueLine.Value;

                    if (keyValueLine.Key.Equals("Skill", StringComparison.OrdinalIgnoreCase))
                        skill = keyValueLine.Value;

                    continue;
                }

                if (parseResult.Document.Lines[index] is IniFlagLine)
                    flagCount++;
            }

            var usage = readResult.Usages.FirstOrDefault(usage => usage.Id == block.UsageId);

            rows.Add(new UsageRow
            {
                Id = block.UsageId,
                ItemTool = !string.IsNullOrWhiteSpace(itemTool) ? itemTool : usage?.ItemTool ?? "",
                ItemFocus = !string.IsNullOrWhiteSpace(itemFocus) ? itemFocus : usage?.ItemFocus ?? "",
                Skill = !string.IsNullOrWhiteSpace(skill) ? skill : usage?.Skill ?? "",
                FieldCount = fieldCount,
                FlagCount = flagCount,
                UnknownCount = usage?.UnknownFields.Count ?? 0,
                IssueCount = usage?.Issues.Count ?? 0
            });
        }

        return rows
            .OrderBy(row => row.Id)
            .ToList();
    }

    public List<UsageFieldRow> LoadFieldRows(
        string usageIniPath,
        int usageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usageIniPath);

        IniParseResult parseResult = IniParser.ParseFile(usageIniPath);

        UsageBlock? block = FindUsageBlock(parseResult.Document, usageId);

        if (block is null)
            return new List<UsageFieldRow>();

        var rows = new List<UsageFieldRow>();

        for (int index = block.Value.StartIndex; index < block.Value.EndIndexExclusive; index++)
        {
            if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
            {
                rows.Add(new UsageFieldRow
                {
                    Type = "Field",
                    Key = keyValueLine.Key,
                    Value = keyValueLine.Value,
                    Enabled = true,
                    IsKnown = true
                });

                continue;
            }

            if (parseResult.Document.Lines[index] is IniFlagLine flagLine)
            {
                rows.Add(new UsageFieldRow
                {
                    Type = "Flag",
                    Key = flagLine.Name,
                    Value = "",
                    Enabled = true,
                    IsKnown = true
                });
            }
        }

        return rows;
    }

    public SaveValidationResult SaveUsageRows(
        string usageIniPath,
        int usageId,
        IReadOnlyList<UsageFieldRow> editedRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usageIniPath);
        ArgumentNullException.ThrowIfNull(editedRows);

        return _saveOperationService.SaveWithBackup(
            usageIniPath,
            saveAction: () =>
            {
                IniParseResult parseResult = IniParser.ParseFile(usageIniPath);

                ApplyUsageRowsToDocument(
                    parseResult.Document,
                    usageId,
                    editedRows);

                IniWriter.WriteFile(usageIniPath, parseResult.Document);
            },
            validateAction: () =>
            {
                UsageIniReadResult result = UsageIniReader.ReadFile(usageIniPath);

                return (
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static void ApplyUsageRowsToDocument(
        IniDocument document,
        int usageId,
        IReadOnlyList<UsageFieldRow> editedRows)
    {
        UsageBlock? block = FindUsageBlock(document, usageId);

        if (block is null)
            throw new InvalidOperationException($"Could not find item use entry #{usageId}.");

        for (int index = block.Value.EndIndexExclusive - 1; index >= block.Value.StartIndex; index--)
            document.Lines.RemoveAt(index);

        int insertIndex = block.Value.StartIndex;

        foreach (UsageFieldRow row in editedRows)
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

    private static List<UsageBlock> FindUsageBlocks(IniDocument document)
    {
        var blocks = new List<UsageBlock>();
        int usageId = 0;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsUsageStartLine(keyValueLine))
                continue;

            usageId++;

            int endIndexExclusive = document.Lines.Count;

            for (int nextIndex = index + 1; nextIndex < document.Lines.Count; nextIndex++)
            {
                if (document.Lines[nextIndex] is not IniKeyValueLine nextKeyValueLine)
                    continue;

                if (IsUsageStartLine(nextKeyValueLine))
                {
                    endIndexExclusive = nextIndex;
                    break;
                }
            }

            blocks.Add(new UsageBlock(
                UsageId: usageId,
                StartIndex: index,
                EndIndexExclusive: endIndexExclusive));
        }

        return blocks;
    }

    private static UsageBlock? FindUsageBlock(
        IniDocument document,
        int usageId)
    {
        return FindUsageBlocks(document)
            .FirstOrDefault(block => block.UsageId == usageId);
    }

    private static bool IsUsageStartLine(IniKeyValueLine line)
    {
        return line.Key.Equals("ItemTool", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct UsageBlock(
        int UsageId,
        int StartIndex,
        int EndIndexExclusive);
}