using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Safely updates existing RPGWO monster.ini documents while preserving original formatting,
/// comments, unknown fields, flags, and monster ordering.
/// 
/// This writer does not rebuild monster.ini from scratch. It edits targeted lines inside
/// an existing IniDocument and lets IniWriter preserve the rest.
/// </summary>
public static class MonsterIniWriter
{
    public static bool WriteUpdatedFieldToFile(
        string inputPath,
        string outputPath,
        int monsterId,
        string fieldName,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = SetMonsterField(
            parseResult.Document,
            monsterId,
            fieldName,
            value);

        if (!updated)
            return false;

        IniWriter.WriteFile(outputPath, parseResult.Document);
        return true;
    }

    public static bool SetMonsterField(
        IniDocument document,
        int monsterId,
        string fieldName,
        string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        MonsterBlock? block = FindMonsterBlock(document, monsterId);

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

    public static bool RemoveMonsterField(
        IniDocument document,
        int monsterId,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        MonsterBlock? block = FindMonsterBlock(document, monsterId);

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

    public static bool SetMonsterFlag(
        IniDocument document,
        int monsterId,
        string flagName,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        MonsterBlock? block = FindMonsterBlock(document, monsterId);

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

    public static bool ContainsMonster(
        IniDocument document,
        int monsterId)
    {
        ArgumentNullException.ThrowIfNull(document);
        return FindMonsterBlock(document, monsterId) is not null;
    }

    private static MonsterBlock? FindMonsterBlock(
        IniDocument document,
        int monsterId)
    {
        int startIndex = -1;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsMonsterMarker(keyValueLine))
                continue;

            if (!int.TryParse(keyValueLine.Value.Trim(), out int foundMonsterId))
                continue;

            if (foundMonsterId != monsterId)
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

            if (!IsMonsterMarker(keyValueLine))
                continue;

            endIndexExclusive = index;
            break;
        }

        return new MonsterBlock(
            StartIndex: startIndex,
            EndIndexExclusive: endIndexExclusive);
    }

    private static bool IsMonsterMarker(IniKeyValueLine line)
    {
        return line.Key.Equals("Monster", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("MonsterID", StringComparison.OrdinalIgnoreCase);
    }

    private static IniKeyValueLine? FindFieldInBlock(
        IniDocument document,
        MonsterBlock block,
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
        MonsterBlock block,
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
        MonsterBlock block,
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
        MonsterBlock block,
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
        MonsterBlock block)
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

    private readonly record struct MonsterBlock(
        int StartIndex,
        int EndIndexExclusive);
}