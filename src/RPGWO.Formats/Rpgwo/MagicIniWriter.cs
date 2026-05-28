using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Safely updates existing RPGWO magic.ini documents while preserving original formatting,
/// comments, unknown fields, flags, and spell ordering.
/// 
/// magic.ini uses Spell=&lt;id&gt; as the start of each spell block.
/// </summary>
public static class MagicIniWriter
{
    public static bool WriteUpdatedFieldToFile(
        string inputPath,
        string outputPath,
        int spellId,
        string fieldName,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = SetMagicField(
            parseResult.Document,
            spellId,
            fieldName,
            value);

        if (!updated)
            return false;

        IniWriter.WriteFile(outputPath, parseResult.Document);
        return true;
    }

    public static bool SetMagicField(
        IniDocument document,
        int spellId,
        string fieldName,
        string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        MagicBlock? block = FindMagicBlock(document, spellId);

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

    public static bool RemoveMagicField(
        IniDocument document,
        int spellId,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        MagicBlock? block = FindMagicBlock(document, spellId);

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

    public static bool SetMagicFlag(
        IniDocument document,
        int spellId,
        string flagName,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        MagicBlock? block = FindMagicBlock(document, spellId);

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

    public static bool ContainsMagic(
        IniDocument document,
        int spellId)
    {
        ArgumentNullException.ThrowIfNull(document);
        return FindMagicBlock(document, spellId) is not null;
    }

    private static MagicBlock? FindMagicBlock(
        IniDocument document,
        int spellId)
    {
        int startIndex = -1;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsSpellMarker(keyValueLine))
                continue;

            if (!int.TryParse(keyValueLine.Value.Trim(), out int foundSpellId))
                continue;

            if (foundSpellId != spellId)
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

            if (!IsSpellMarker(keyValueLine))
                continue;

            endIndexExclusive = index;
            break;
        }

        return new MagicBlock(
            StartIndex: startIndex,
            EndIndexExclusive: endIndexExclusive);
    }

    private static bool IsSpellMarker(IniKeyValueLine line)
    {
        return line.Key.Equals("Spell", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("SpellID", StringComparison.OrdinalIgnoreCase);
    }

    private static IniKeyValueLine? FindFieldInBlock(
        IniDocument document,
        MagicBlock block,
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
        MagicBlock block,
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
        MagicBlock block,
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
        MagicBlock block,
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
        MagicBlock block)
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

    private readonly record struct MagicBlock(
        int StartIndex,
        int EndIndexExclusive);
}