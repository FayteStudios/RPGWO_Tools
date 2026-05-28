using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Loads and saves animation.ini data for the GUI animation editor.
/// This reads editable rows directly from the INI document so repeated Frame/Sound lines are preserved as rows.
/// </summary>
public sealed class AnimationEditorService
{
    private readonly SaveOperationService _saveOperationService = new();

    public List<AnimationRow> LoadAnimationRows(string animationIniPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(animationIniPath);

        IniParseResult parseResult = IniParser.ParseFile(animationIniPath);
        AnimationIniReadResult readResult = AnimationIniReader.ReadFile(animationIniPath);

        var rows = new List<AnimationRow>();

        foreach (AnimationBlock block in FindAnimationBlocks(parseResult.Document))
        {
            string name = "";
            int frameCount = 0;
            int soundCount = 0;
            bool rotational = false;

            for (int index = block.StartIndex + 1; index < block.EndIndexExclusive; index++)
            {
                if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
                {
                    if (keyValueLine.Key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        name = keyValueLine.Value;

                    if (keyValueLine.Key.Equals("Frame", StringComparison.OrdinalIgnoreCase))
                        frameCount++;

                    if (keyValueLine.Key.Equals("Sound", StringComparison.OrdinalIgnoreCase))
                        soundCount++;

                    continue;
                }

                if (parseResult.Document.Lines[index] is IniFlagLine flagLine)
                {
                    if (flagLine.Name.Equals("Rotational", StringComparison.OrdinalIgnoreCase))
                        rotational = true;
                }
            }

            var animation = readResult.Animations.FirstOrDefault(animation => animation.Id == block.AnimationId);

            rows.Add(new AnimationRow
            {
                Id = block.AnimationId,
                Name = !string.IsNullOrWhiteSpace(name)
                    ? name
                    : animation?.Name ?? "",
                FrameCount = frameCount,
                SoundCount = soundCount,
                Rotational = rotational,
                UnknownCount = animation?.UnknownFields.Count ?? 0,
                IssueCount = animation?.Issues.Count ?? 0
            });
        }

        return rows
            .OrderBy(row => row.Id)
            .ToList();
    }

    public List<AnimationFieldRow> LoadFieldRows(
        string animationIniPath,
        int animationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(animationIniPath);

        IniParseResult parseResult = IniParser.ParseFile(animationIniPath);

        AnimationBlock? block = FindAnimationBlock(parseResult.Document, animationId);

        if (block is null)
            return new List<AnimationFieldRow>();

        var rows = new List<AnimationFieldRow>();

        for (int index = block.Value.StartIndex + 1; index < block.Value.EndIndexExclusive; index++)
        {
            if (parseResult.Document.Lines[index] is IniKeyValueLine keyValueLine)
            {
                rows.Add(new AnimationFieldRow
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
                rows.Add(new AnimationFieldRow
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

    public SaveValidationResult SaveAnimationRows(
        string animationIniPath,
        int animationId,
        IReadOnlyList<AnimationFieldRow> editedRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(animationIniPath);
        ArgumentNullException.ThrowIfNull(editedRows);

        return _saveOperationService.SaveWithBackup(
            animationIniPath,
            saveAction: () =>
            {
                IniParseResult parseResult = IniParser.ParseFile(animationIniPath);

                ApplyAnimationRowsToDocument(
                    parseResult.Document,
                    animationId,
                    editedRows);

                IniWriter.WriteFile(animationIniPath, parseResult.Document);
            },
            validateAction: () =>
            {
                AnimationIniReadResult result = AnimationIniReader.ReadFile(animationIniPath);

                return (
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static void ApplyAnimationRowsToDocument(
        IniDocument document,
        int animationId,
        IReadOnlyList<AnimationFieldRow> editedRows)
    {
        AnimationBlock? block = FindAnimationBlock(document, animationId);

        if (block is null)
            throw new InvalidOperationException($"Could not find Animation={animationId}.");

        // Remove existing active/comment/blank lines inside this animation block, except Animation=<id>.
        // This keeps the save behavior consistent with the first Skill editor version.
        for (int index = block.Value.EndIndexExclusive - 1; index > block.Value.StartIndex; index--)
            document.Lines.RemoveAt(index);

        int insertIndex = block.Value.StartIndex + 1;

        foreach (AnimationFieldRow row in editedRows)
        {
            if (string.IsNullOrWhiteSpace(row.Key))
                continue;
            if (!row.Enabled)
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
            || key.Equals("Frame", StringComparison.OrdinalIgnoreCase)
            || key.Equals("FrameSize", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Sound", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKnownFlag(string flag)
    {
        return flag.Equals("Rotational", StringComparison.OrdinalIgnoreCase);
    }

    private static List<AnimationBlock> FindAnimationBlocks(IniDocument document)
    {
        var blocks = new List<AnimationBlock>();

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsAnimationMarker(keyValueLine))
                continue;

            if (!int.TryParse(keyValueLine.Value.Trim(), out int animationId))
                continue;

            int endIndexExclusive = document.Lines.Count;

            for (int nextIndex = index + 1; nextIndex < document.Lines.Count; nextIndex++)
            {
                if (document.Lines[nextIndex] is not IniKeyValueLine nextKeyValueLine)
                    continue;

                if (IsAnimationMarker(nextKeyValueLine))
                {
                    endIndexExclusive = nextIndex;
                    break;
                }
            }

            blocks.Add(new AnimationBlock(
                AnimationId: animationId,
                StartIndex: index,
                EndIndexExclusive: endIndexExclusive));
        }

        return blocks;
    }

    private static AnimationBlock? FindAnimationBlock(
        IniDocument document,
        int animationId)
    {
        return FindAnimationBlocks(document)
            .FirstOrDefault(block => block.AnimationId == animationId);
    }

    private static bool IsAnimationMarker(IniKeyValueLine line)
    {
        return line.Key.Equals("Animation", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("AnimationID", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct AnimationBlock(
        int AnimationId,
        int StartIndex,
        int EndIndexExclusive);
}