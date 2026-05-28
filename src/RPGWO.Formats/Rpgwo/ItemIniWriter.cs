using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Safely updates existing RPGWO item.ini documents while preserving original formatting,
/// comments, unknown fields, flags, and item ordering.
/// 
/// This writer does not rebuild item.ini from scratch. It edits targeted lines inside
/// an existing IniDocument and lets IniWriter preserve the rest.
/// </summary>
public static class ItemIniWriter
{
    /// <summary>
    /// Reads an item.ini file, updates one field on one item, and writes the result to a new file.
    /// </summary>
    public static bool WriteUpdatedFieldToFile(
        string inputPath,
        string outputPath,
        int itemId,
        string fieldName,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = SetItemField(
            parseResult.Document,
            itemId,
            fieldName,
            value);

        if (!updated)
            return false;

        IniWriter.WriteFile(outputPath, parseResult.Document);
        return true;
    }

    /// <summary>
    /// Sets a key/value field for an item.
    /// If the field already exists inside the item block, its value is updated.
    /// If the field does not exist, it is inserted before the next Item= marker.
    /// </summary>
    public static bool SetItemField(
        IniDocument document,
        int itemId,
        string fieldName,
        string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        ItemBlock? block = FindItemBlock(document, itemId);

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

    /// <summary>
    /// Removes all key/value fields with the given name from an item block.
    /// This physically removes those lines from the document.
    /// </summary>
    public static bool RemoveItemField(
        IniDocument document,
        int itemId,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        ItemBlock? block = FindItemBlock(document, itemId);

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

    /// <summary>
    /// Adds or removes a bare item flag such as Stackable, NotMovable, Lockable, etc.
    /// </summary>
    public static bool SetItemFlag(
        IniDocument document,
        int itemId,
        string flagName,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        ItemBlock? block = FindItemBlock(document, itemId);

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

    /// <summary>
    /// Returns true if an item exists in the document.
    /// </summary>
    public static bool ContainsItem(
        IniDocument document,
        int itemId)
    {
        ArgumentNullException.ThrowIfNull(document);
        return FindItemBlock(document, itemId) is not null;
    }

    private static ItemBlock? FindItemBlock(
        IniDocument document,
        int itemId)
    {
        int startIndex = -1;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsItemMarker(keyValueLine))
                continue;

            if (!int.TryParse(keyValueLine.Value.Trim(), out int foundItemId))
                continue;

            if (foundItemId != itemId)
                continue;

            startIndex = index;
            break;
        }

        if (startIndex < 0)
            return null;

        int endIndexExclusive = document.Lines.Count;

        for (int index = startIndex + 1; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsItemMarker(keyValueLine))
                continue;

            endIndexExclusive = index;
            break;
        }

        return new ItemBlock(
            StartIndex: startIndex,
            EndIndexExclusive: endIndexExclusive);
    }

    private static bool IsItemMarker(IniKeyValueLine line)
    {
        return line.Key.Equals("Item", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("ItemID", StringComparison.OrdinalIgnoreCase);
    }

    private static IniKeyValueLine? FindFieldInBlock(
        IniDocument document,
        ItemBlock block,
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
        ItemBlock block,
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
        ItemBlock block,
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
        ItemBlock block,
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

    /// <summary>
    /// Finds a clean insertion point near the end of the item block.
    /// If the item has trailing blank lines, insert before those blanks.
    /// Otherwise insert immediately before the next Item= marker.
    /// </summary>
    private static int FindInsertionIndex(
        IniDocument document,
        ItemBlock block)
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

    private readonly record struct ItemBlock(
        int StartIndex,
        int EndIndexExclusive);
}