using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Safely updates existing RPGWO itemuse.ini documents while preserving original formatting,
/// comments, unknown fields, flags, and block ordering.
///
/// itemuse.ini blocks are started by a bare "Itemuse" line. Since legacy files do not
/// contain explicit IDs, this writer targets usage blocks by their sequential EntryId:
/// first Itemuse block is 1, second is 2, etc.
/// </summary>
public static class UsageIniWriter
{
    public static bool WriteUpdatedFieldToFile(
        string inputPath,
        string outputPath,
        int entryId,
        string fieldName,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = SetUsageField(
            parseResult.Document,
            entryId,
            fieldName,
            value);

        if (!updated)
            return false;

        IniWriter.WriteFile(outputPath, parseResult.Document);
        return true;
    }

    public static bool SetUsageField(
        IniDocument document,
        int entryId,
        string fieldName,
        string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        UsageBlock? block = FindUsageBlock(document, entryId);

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

    public static bool RemoveUsageField(
        IniDocument document,
        int entryId,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        UsageBlock? block = FindUsageBlock(document, entryId);

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

    public static bool SetUsageFlag(
        IniDocument document,
        int entryId,
        string flagName,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        UsageBlock? block = FindUsageBlock(document, entryId);

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

    public static bool ContainsUsage(
        IniDocument document,
        int entryId)
    {
        ArgumentNullException.ThrowIfNull(document);
        return FindUsageBlock(document, entryId) is not null;
    }

    private static UsageBlock? FindUsageBlock(
        IniDocument document,
        int entryId)
    {
        if (entryId <= 0)
            return null;

        int currentEntryId = 0;
        int startIndex = -1;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (!IsUsageMarker(document.Lines[index]))
                continue;

            currentEntryId++;

            if (currentEntryId == entryId)
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
            if (!IsUsageMarker(document.Lines[index]))
                continue;

            endIndexExclusive = index;
            break;
        }

        return new UsageBlock(
            StartIndex: startIndex,
            EndIndexExclusive: endIndexExclusive);
    }

    private static bool IsUsageMarker(IniLine line)
    {
        if (line is IniFlagLine flagLine)
        {
            return flagLine.Name.Equals("Itemuse", StringComparison.OrdinalIgnoreCase)
                || flagLine.Name.Equals("ItemUse", StringComparison.OrdinalIgnoreCase);
        }

        if (line is IniKeyValueLine keyValueLine)
        {
            return keyValueLine.Key.Equals("Itemuse", StringComparison.OrdinalIgnoreCase)
                || keyValueLine.Key.Equals("ItemUse", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static IniKeyValueLine? FindFieldInBlock(
        IniDocument document,
        UsageBlock block,
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
        UsageBlock block,
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
        UsageBlock block,
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
        UsageBlock block,
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
        UsageBlock block)
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

    private readonly record struct UsageBlock(
        int StartIndex,
        int EndIndexExclusive);
}