using RPGWO.Formats.Ini;
using RPGWO.ServerTool.Models;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Builds read-only raw INI previews for selected editor entries.
/// This is used by the GUI to show the actual source block being edited.
/// </summary>
public sealed class IniEntryPreviewService
{
    public string GetWorldPreview(string worldIniPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worldIniPath);

        if (!File.Exists(worldIniPath))
            return "world.ini was not found.";

        return File.ReadAllText(worldIniPath);
    }

    public string GetItemPreview(string itemIniPath, int itemId)
    {
        return GetMarkerBlockPreview(
            itemIniPath,
            itemId,
            "Item",
            "ItemID");
    }

    public string GetMonsterPreview(string monsterIniPath, int monsterId)
    {
        return GetMarkerBlockPreview(
            monsterIniPath,
            monsterId,
            "Monster",
            "MonsterID");
    }

    public string GetSkillPreview(string skillIniPath, int skillId)
    {
        return GetMarkerBlockPreview(
            skillIniPath,
            skillId,
            "Skill",
            "SkillID");
    }

    public string GetMagicPreview(string magicIniPath, int spellId)
    {
        return GetMarkerBlockPreview(
            magicIniPath,
            spellId,
            "Spell",
            "SpellID",
            "Magic");
    }

    public string GetAnimationPreview(string animationIniPath, int animationId)
    {
        return GetMarkerBlockPreview(
            animationIniPath,
            animationId,
            "Animation",
            "AnimationID");
    }

    public string GetTreasurePreview(string treasureIniPath, int treasureId)
    {
        // treasure.ini commonly uses non-numeric names like Treasure=LowWeapon.
        // The reader assigns fallback IDs based on encounter order, so preview does the same.
        return GetGeneratedIndexBlockPreview(
            treasureIniPath,
            treasureId,
            line => line.Key.Equals("Treasure", StringComparison.OrdinalIgnoreCase)
                || line.Key.Equals("TreasureID", StringComparison.OrdinalIgnoreCase));
    }

    public string GetUsagePreview(string usageIniPath, int usageId)
    {
        // itemuse.ini entries start with ItemTool=.
        return GetGeneratedIndexBlockPreview(
            usageIniPath,
            usageId,
            line => line.Key.Equals("ItemTool", StringComparison.OrdinalIgnoreCase));
    }

    public string GetMultiUsePreview(string multiUseIniPath, int multiUseId, int expectedRecipeCount)
    {
        IniParseResult parseResult = IniParser.ParseFile(multiUseIniPath);

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

        List<BlockRange> bestBlocks = new();

        foreach (string candidateStartKey in candidateStartKeys)
        {
            List<BlockRange> candidateBlocks = FindGeneratedBlocks(
                parseResult.Document,
                line => line.Key.Equals(candidateStartKey, StringComparison.OrdinalIgnoreCase),
                allowExplicitNumericId: IsExplicitMultiUseIdMarker(candidateStartKey));

            if (candidateBlocks.Count == 0)
                continue;

            if (candidateBlocks.Count == expectedRecipeCount)
                return BlockText(parseResult.Document, candidateBlocks, multiUseId);

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

        if (bestBlocks.Count == 0)
            return $"Could not find multi-use recipe #{multiUseId}.";

        return BlockText(parseResult.Document, bestBlocks, multiUseId);
    }

    private static string GetMarkerBlockPreview(
        string iniPath,
        int id,
        params string[] markerKeys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniPath);

        if (!File.Exists(iniPath))
            return "INI file was not found.";

        IniParseResult parseResult = IniParser.ParseFile(iniPath);

        List<BlockRange> blocks = FindMarkerBlocks(
            parseResult.Document,
            markerKeys);

        return BlockText(parseResult.Document, blocks, id);
    }

    private static string GetGeneratedIndexBlockPreview(
        string iniPath,
        int generatedId,
        Func<IniKeyValueLine, bool> isStartLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniPath);

        if (!File.Exists(iniPath))
            return "INI file was not found.";

        IniParseResult parseResult = IniParser.ParseFile(iniPath);

        List<BlockRange> blocks = FindGeneratedBlocks(
            parseResult.Document,
            isStartLine,
            allowExplicitNumericId: false);

        return BlockText(parseResult.Document, blocks, generatedId);
    }

    private static List<BlockRange> FindMarkerBlocks(
        IniDocument document,
        IReadOnlyCollection<string> markerKeys)
    {
        var blocks = new List<BlockRange>();

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!markerKeys.Any(marker =>
                    keyValueLine.Key.Equals(marker, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (!int.TryParse(keyValueLine.Value.Trim(), out int id))
                continue;

            int endIndexExclusive = document.Lines.Count;

            for (int nextIndex = index + 1; nextIndex < document.Lines.Count; nextIndex++)
            {
                if (document.Lines[nextIndex] is not IniKeyValueLine nextKeyValueLine)
                    continue;

                if (markerKeys.Any(marker =>
                        nextKeyValueLine.Key.Equals(marker, StringComparison.OrdinalIgnoreCase)))
                {
                    endIndexExclusive = nextIndex;
                    break;
                }
            }

            blocks.Add(new BlockRange(
                Id: id,
                StartIndex: index,
                EndIndexExclusive: endIndexExclusive));
        }

        return blocks;
    }

    private static List<BlockRange> FindGeneratedBlocks(
        IniDocument document,
        Func<IniKeyValueLine, bool> isStartLine,
        bool allowExplicitNumericId)
    {
        var blocks = new List<BlockRange>();
        int generatedId = 0;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!isStartLine(keyValueLine))
                continue;

            int id;

            if (allowExplicitNumericId &&
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

                if (isStartLine(nextKeyValueLine))
                {
                    endIndexExclusive = nextIndex;
                    break;
                }
            }

            blocks.Add(new BlockRange(
                Id: id,
                StartIndex: index,
                EndIndexExclusive: endIndexExclusive));
        }

        return blocks;
    }

    private static string BlockText(
        IniDocument document,
        IReadOnlyList<BlockRange> blocks,
        int id)
    {
        BlockRange? block = blocks.FirstOrDefault(block => block.Id == id);

        if (block is null)
            return $"Could not find entry #{id}.";

        return BuildTextFromRange(
            document,
            block.Value.StartIndex,
            block.Value.EndIndexExclusive);
    }

    private static string BuildTextFromRange(
        IniDocument document,
        int startIndex,
        int endIndexExclusive)
    {
        var lines = new List<string>();

        for (int index = startIndex; index < endIndexExclusive; index++)
            lines.Add(ToSourceText(document.Lines[index]));

        return string.Join(Environment.NewLine, lines);
    }

    private static string ToSourceText(IniLine line)
    {
        return line switch
        {
            IniKeyValueLine keyValueLine => $"{keyValueLine.Key}{keyValueLine.Separator}{keyValueLine.Value}",
            IniFlagLine flagLine => flagLine.Name,
            IniCommentLine commentLine => commentLine.Text,
            IniBlankLine => "",
            _ => line.OriginalText
        };
    }

    private static bool IsExplicitMultiUseIdMarker(string key)
    {
        return key.Equals("MultiUse", StringComparison.OrdinalIgnoreCase)
            || key.Equals("MultiUseID", StringComparison.OrdinalIgnoreCase)
            || key.Equals("Recipe", StringComparison.OrdinalIgnoreCase)
            || key.Equals("RecipeID", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct BlockRange(
        int Id,
        int StartIndex,
        int EndIndexExclusive);
}