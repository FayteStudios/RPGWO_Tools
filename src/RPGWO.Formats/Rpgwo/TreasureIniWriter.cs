using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Safely updates existing RPGWO treasure.ini documents while preserving original formatting,
/// comments, unknown fields, flags, and treasure ordering.
///
/// Some treasure.ini files use named tables such as Treasure=LowWeapon instead of numeric IDs.
/// This writer targets treasure blocks by their sequential table ID:
/// first Treasure block is 1, second is 2, etc.
/// </summary>
public static class TreasureIniWriter
{
    public static bool WriteUpdatedFieldToFile(
        string inputPath,
        string outputPath,
        int treasureId,
        string fieldName,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = SetTreasureField(
            parseResult.Document,
            treasureId,
            fieldName,
            value);

        if (!updated)
            return false;

        IniWriter.WriteFile(outputPath, parseResult.Document);
        return true;
    }

    public static bool SetTreasureField(
        IniDocument document,
        int treasureId,
        string fieldName,
        string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        TreasureBlock? block = FindTreasureBlock(document, treasureId);

        if (block is null)
            return false;

        IniKeyValueLine? existingLine = FindFieldInBlock(
            document,
            block.Value,
            fieldName);

        if (existingLine is not null)
        {
            existingLine.Value = value;
            existingLine.IsModified = true;
            return true;
        }

        InsertFieldAtEndOfBlock(
            document,
            block.Value,
            fieldName,
            value);

        return true;
    }

    public static bool RemoveTreasureField(
        IniDocument document,
        int treasureId,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        TreasureBlock? block = FindTreasureBlock(document, treasureId);

        if (block is null)
            return false;

        bool removedAny = false;

        for (int index = block.Value.EndIndexExclusive - 1; index > block.Value.StartIndex; index--)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!keyValueLine.Key.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                continue;

            document.Lines.RemoveAt(index);
            removedAny = true;
        }

        return removedAny;
    }

    public static bool SetTreasureFlag(
        IniDocument document,
        int treasureId,
        string flagName,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        TreasureBlock? block = FindTreasureBlock(document, treasureId);

        if (block is null)
            return false;

        List<int> matchingFlagIndexes = FindFlagIndexesInBlock(
            document,
            block.Value,
            flagName);

        if (enabled)
        {
            if (matchingFlagIndexes.Count > 0)
                return true;

            InsertFlagAtEndOfBlock(document, block.Value, flagName);
            return true;
        }

        for (int i = matchingFlagIndexes.Count - 1; i >= 0; i--)
            document.Lines.RemoveAt(matchingFlagIndexes[i]);

        return true;
    }

    public static bool ContainsTreasure(
        IniDocument document,
        int treasureId)
    {
        ArgumentNullException.ThrowIfNull(document);
        return FindTreasureBlock(document, treasureId) is not null;
    }

    private static TreasureBlock? FindTreasureBlock(
        IniDocument document,
        int treasureId)
    {
        if (treasureId <= 0)
            return null;

        int currentTreasureId = 0;
        int startIndex = -1;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsTreasureMarker(keyValueLine))
                continue;

            currentTreasureId++;

            if (currentTreasureId == treasureId)
            {
                startIndex = index;
                break;
            }
        }

        if (startIndex < 0)
            return null;

        int endIndexExclusive = document.Lines.Count;

        for (int index = startIndex + 1; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsTreasureMarker(keyValueLine))
                continue;

            endIndexExclusive = index;
            break;
        }

        return new TreasureBlock(
            StartIndex: startIndex,
            EndIndexExclusive: endIndexExclusive);
    }

    private static bool IsTreasureMarker(IniKeyValueLine line)
    {
        return line.Key.Equals("Treasure", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("TreasureID", StringComparison.OrdinalIgnoreCase);
    }

    private static IniKeyValueLine? FindFieldInBlock(
        IniDocument document,
        TreasureBlock block,
        string fieldName)
    {
        for (int index = block.StartIndex + 1; index < block.EndIndexExclusive; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (keyValueLine.Key.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                return keyValueLine;
        }

        return null;
    }

    private static List<int> FindFlagIndexesInBlock(
        IniDocument document,
        TreasureBlock block,
        string flagName)
    {
        var indexes = new List<int>();

        for (int index = block.StartIndex + 1; index < block.EndIndexExclusive; index++)
        {
            if (document.Lines[index] is not IniFlagLine flagLine)
                continue;

            if (flagLine.Name.Equals(flagName, StringComparison.OrdinalIgnoreCase))
                indexes.Add(index);
        }

        return indexes;
    }

    private static void InsertFieldAtEndOfBlock(
        IniDocument document,
        TreasureBlock block,
        string fieldName,
        string value)
    {
        int insertIndex = FindInsertionIndex(document, block);

        document.Lines.Insert(
            insertIndex,
            new IniKeyValueLine
            {
                LineNumber = 0,
                OriginalText = $"{fieldName}={value}",
                Key = fieldName,
                Value = value,
                Separator = "=",
                IsModified = false
            });
    }

    private static void InsertFlagAtEndOfBlock(
        IniDocument document,
        TreasureBlock block,
        string flagName)
    {
        int insertIndex = FindInsertionIndex(document, block);

        document.Lines.Insert(
            insertIndex,
            new IniFlagLine
            {
                LineNumber = 0,
                OriginalText = flagName,
                Name = flagName
            });
    }

    private static int FindInsertionIndex(
        IniDocument document,
        TreasureBlock block)
    {
        int insertIndex = block.EndIndexExclusive;

        for (int index = block.EndIndexExclusive - 1; index > block.StartIndex; index--)
        {
            if (document.Lines[index] is IniBlankLine)
            {
                insertIndex = index;
                continue;
            }

            break;
        }

        return insertIndex;
    }

    private readonly record struct TreasureBlock(
        int StartIndex,
        int EndIndexExclusive);
}