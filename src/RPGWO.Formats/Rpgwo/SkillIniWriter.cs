using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Safely updates existing RPGWO skill.ini documents while preserving original formatting,
/// comments, unknown fields, flags, and skill ordering.
/// 
/// This writer does not rebuild skill.ini from scratch. It edits targeted lines inside
/// an existing IniDocument and lets IniWriter preserve the rest.
/// </summary>
public static class SkillIniWriter
{
    public static bool WriteUpdatedFieldToFile(
        string inputPath,
        string outputPath,
        int skillId,
        string fieldName,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = SetSkillField(
            parseResult.Document,
            skillId,
            fieldName,
            value);

        if (!updated)
            return false;

        IniWriter.WriteFile(outputPath, parseResult.Document);
        return true;
    }

    public static bool SetSkillField(
        IniDocument document,
        int skillId,
        string fieldName,
        string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        SkillBlock? block = FindSkillBlock(document, skillId);

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

    public static bool RemoveSkillField(
        IniDocument document,
        int skillId,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        SkillBlock? block = FindSkillBlock(document, skillId);

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

    public static bool SetSkillFlag(
        IniDocument document,
        int skillId,
        string flagName,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        SkillBlock? block = FindSkillBlock(document, skillId);

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

    public static bool ContainsSkill(
        IniDocument document,
        int skillId)
    {
        ArgumentNullException.ThrowIfNull(document);
        return FindSkillBlock(document, skillId) is not null;
    }

    private static SkillBlock? FindSkillBlock(
        IniDocument document,
        int skillId)
    {
        int startIndex = -1;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!IsSkillMarker(keyValueLine))
                continue;

            if (!int.TryParse(keyValueLine.Value.Trim(), out int foundSkillId))
                continue;

            if (foundSkillId != skillId)
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

            if (!IsSkillMarker(keyValueLine))
                continue;

            endIndexExclusive = index;
            break;
        }

        return new SkillBlock(
            StartIndex: startIndex,
            EndIndexExclusive: endIndexExclusive);
    }

    private static bool IsSkillMarker(IniKeyValueLine line)
    {
        return line.Key.Equals("Skill", StringComparison.OrdinalIgnoreCase)
            || line.Key.Equals("SkillID", StringComparison.OrdinalIgnoreCase);
    }

    private static IniKeyValueLine? FindFieldInBlock(
        IniDocument document,
        SkillBlock block,
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
        SkillBlock block,
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
        SkillBlock block,
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
        SkillBlock block,
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
        SkillBlock block)
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

    private readonly record struct SkillBlock(
        int StartIndex,
        int EndIndexExclusive);
}