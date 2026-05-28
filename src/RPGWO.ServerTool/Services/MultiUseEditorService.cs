using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Loads and saves multiuse.ini data for the GUI multi-use editor.
/// This service detects recipe boundaries defensively by matching candidate block counts
/// against the typed MultiUseIniReader result.
/// </summary>
public sealed class MultiUseEditorService
{
    private readonly SaveOperationService _saveOperationService = new();

    public List<MultiUseRow> LoadMultiUseRows(string multiUseIniPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(multiUseIniPath);

        IniParseResult parseResult = IniParser.ParseFile(multiUseIniPath);
        MultiUseIniReadResult readResult = MultiUseIniReader.ReadFile(multiUseIniPath);

        List<MultiUseBlock> blocks = FindMultiUseBlocks(
            parseResult.Document,
            readResult.Recipes.Count);

        var rows = new List<MultiUseRow>();

        foreach (MultiUseBlock block in blocks)
        {
            string successItem = "";
            string focusItem = "";
            string skill = "";
            int fieldCount = 0;
            int flagCount = 0;

            for (int index = block.StartIndex; index < block.EndIndexExclusive; index++)
            {
                if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
                {
                    fieldCount++;

                    if (keyValueLine.Key.Equals("SuccessItem", StringComparison.OrdinalIgnoreCase)
                        || keyValueLine.Key.Equals("SuccessItem1", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(successItem))
                            successItem = keyValueLine.Value;
                    }

                    if (keyValueLine.Key.Equals("FocusItem", StringComparison.OrdinalIgnoreCase)
                        || keyValueLine.Key.Equals("ItemFocus", StringComparison.OrdinalIgnoreCase)
                        || keyValueLine.Key.Equals("Focus", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(focusItem))
                            focusItem = keyValueLine.Value;
                    }

                    if (keyValueLine.Key.Equals("Skill", StringComparison.OrdinalIgnoreCase))
                        skill = keyValueLine.Value;

                    continue;
                }

                if (parseResult.Document.Lines[index] is IniFlagLine)
                    flagCount++;
            }

            var recipe = readResult.Recipes.FirstOrDefault(recipe => recipe.Id == block.MultiUseId);

            rows.Add(new MultiUseRow
            {
                Id = block.MultiUseId,
                SuccessItem = !string.IsNullOrWhiteSpace(successItem) ? successItem : recipe?.SuccessItem ?? "",
                FocusItem = !string.IsNullOrWhiteSpace(focusItem) ? focusItem : recipe?.FocusItem ?? "",
                Skill = !string.IsNullOrWhiteSpace(skill) ? skill : recipe?.Skill ?? "",
                FieldCount = fieldCount,
                FlagCount = flagCount,
                UnknownCount = recipe?.UnknownFields.Count ?? 0,
                IssueCount = recipe?.Issues.Count ?? 0
            });
        }

        return rows
            .OrderBy(row => row.Id)
            .ToList();
    }

    public List<MultiUseFieldRow> LoadFieldRows(
        string multiUseIniPath,
        int multiUseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(multiUseIniPath);

        IniParseResult parseResult = IniParser.ParseFile(multiUseIniPath);
        MultiUseIniReadResult readResult = MultiUseIniReader.ReadFile(multiUseIniPath);

        MultiUseBlock? block = FindMultiUseBlock(
            parseResult.Document,
            readResult.Recipes.Count,
            multiUseId);

        if (block is null)
            return new List<MultiUseFieldRow>();

        var rows = new List<MultiUseFieldRow>();

        for (int index = block.Value.StartIndex; index < block.Value.EndIndexExclusive; index++)
        {
            if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
            {
                rows.Add(new MultiUseFieldRow
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
                rows.Add(new MultiUseFieldRow
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

    public SaveValidationResult SaveMultiUseRows(
        string multiUseIniPath,
        int multiUseId,
        IReadOnlyList<MultiUseFieldRow> editedRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(multiUseIniPath);
        ArgumentNullException.ThrowIfNull(editedRows);

        return _saveOperationService.SaveWithBackup(
            multiUseIniPath,
            saveAction: () =>
            {
                IniParseResult parseResult = IniParser.ParseFile(multiUseIniPath);
                MultiUseIniReadResult readResult = MultiUseIniReader.ReadFile(multiUseIniPath);

                ApplyMultiUseRowsToDocument(
                    parseResult.Document,
                    readResult.Recipes.Count,
                    multiUseId,
                    editedRows);

                IniWriter.WriteFile(multiUseIniPath, parseResult.Document);
            },
            validateAction: () =>
            {
                MultiUseIniReadResult result = MultiUseIniReader.ReadFile(multiUseIniPath);

                return (
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static void ApplyMultiUseRowsToDocument(
        IniDocument document,
        int expectedRecipeCount,
        int multiUseId,
        IReadOnlyList<MultiUseFieldRow> editedRows)
    {
        MultiUseBlock? block = FindMultiUseBlock(
            document,
            expectedRecipeCount,
            multiUseId);

        if (block is null)
            throw new InvalidOperationException($"Could not find multi-use recipe #{multiUseId}.");

        for (int index = block.Value.EndIndexExclusive - 1; index >= block.Value.StartIndex; index--)
            document.Lines.RemoveAt(index);

        int insertIndex = block.Value.StartIndex;

        foreach (MultiUseFieldRow row in editedRows)
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

    private static MultiUseBlock? FindMultiUseBlock(
        IniDocument document,
        int expectedRecipeCount,
        int multiUseId)
    {
        return FindMultiUseBlocks(document, expectedRecipeCount)
            .FirstOrDefault(block => block.MultiUseId == multiUseId);
    }

    private static List<MultiUseBlock> FindMultiUseBlocks(
        IniDocument document,
        int expectedRecipeCount)
    {
        string[] candidateStartKeys =
        {
            "MultiUse",
            "MultiUseID",
            "Recipe",
            "RecipeID",
            "ItemTool",
            "SuccessItem",
            "SuccessItem1",
            "FocusItem",
            "ItemFocus"
        };

        List<MultiUseBlock> bestBlocks = new();

        foreach (string candidateStartKey in candidateStartKeys)
        {
            List<MultiUseBlock> candidateBlocks = FindBlocksByStartKey(
                document,
                candidateStartKey);

            if (candidateBlocks.Count == 0)
                continue;

            if (candidateBlocks.Count == expectedRecipeCount)
                return candidateBlocks;

            if (bestBlocks.Count == 0)
            {
                bestBlocks = candidateBlocks;
                continue;
            }

            int currentDistance = Math.Abs(candidateBlocks.Count - expectedRecipeCount);
            int bestDistance = Math.Abs(bestBlocks.Count - expectedRecipeCount);

            if (currentDistance < bestDistance)
                bestBlocks = candidateBlocks;
        }

        return bestBlocks;
    }

    private static List<MultiUseBlock> FindBlocksByStartKey(
        IniDocument document,
        string startKey)
    {
        var blocks = new List<MultiUseBlock>();
        int generatedId = 0;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!keyValueLine.Key.Equals(startKey, StringComparison.OrdinalIgnoreCase))
                continue;

            int id;

            if (IsExplicitIdMarker(startKey) &&
                int.TryParse(keyValueLine.Value.Trim(), out int parsedId))
            {
                id = parsedId;
            }
            else
            {
                id = ++generatedId;
            }

            int endIndexExclusive = document.Lines.Count;

            for (int nextIndex = index + 1; nextIndex < document.Lines.Count; nextIndex++)
            {
                if (document.Lines[nextIndex] is not IniKeyValueLine nextKeyValueLine)
                    continue;

                if (nextKeyValueLine.Key.Equals(startKey, StringComparison.OrdinalIgnoreCase))
                {
                    endIndexExclusive = nextIndex;
                    break;
                }
            }

            blocks.Add(new MultiUseBlock(
                MultiUseId: id,
                StartIndex: index,
                EndIndexExclusive: endIndexExclusive,
                StartKey: startKey));
        }

        return blocks;
    }

    private static bool IsExplicitIdMarker(string key)
    {
        return key.Equals("MultiUse", StringComparison.OrdinalIgnoreCase)
            || key.Equals("MultiUseID", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Recipe", StringComparison.OrdinalIgnoreCase)
            || key.Equals("RecipeID", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct MultiUseBlock(
        int MultiUseId,
        int StartIndex,
        int EndIndexExclusive,
        string StartKey);
}