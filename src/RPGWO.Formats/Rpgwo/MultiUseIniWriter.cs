using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Safely updates existing RPGWO multiuse.ini documents while preserving original formatting,
/// comments, unknown fields, flags, and recipe ordering.
///
/// multiuse.ini blocks are started by a bare "MultiUse" line. Since legacy files do not
/// contain explicit IDs, this writer targets recipe blocks by their sequential RecipeId:
/// first MultiUse block is 1, second is 2, etc.
/// </summary>
public static class MultiUseIniWriter
{
    public static bool WriteUpdatedFieldToFile(
        string inputPath,
        string outputPath,
        int recipeId,
        string fieldName,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = SetMultiUseField(
            parseResult.Document,
            recipeId,
            fieldName,
            value);

        if (!updated)
            return false;

        IniWriter.WriteFile(outputPath, parseResult.Document);
        return true;
    }

    public static bool SetMultiUseField(
        IniDocument document,
        int recipeId,
        string fieldName,
        string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        MultiUseBlock? block = FindMultiUseBlock(document, recipeId);

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

    public static bool RemoveMultiUseField(
        IniDocument document,
        int recipeId,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        MultiUseBlock? block = FindMultiUseBlock(document, recipeId);

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

    public static bool SetMultiUseFlag(
        IniDocument document,
        int recipeId,
        string flagName,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        MultiUseBlock? block = FindMultiUseBlock(document, recipeId);

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

    public static bool ContainsMultiUse(
        IniDocument document,
        int recipeId)
    {
        ArgumentNullException.ThrowIfNull(document);
        return FindMultiUseBlock(document, recipeId) is not null;
    }

    private static MultiUseBlock? FindMultiUseBlock(
        IniDocument document,
        int recipeId)
    {
        if (recipeId <= 0)
            return null;

        int currentRecipeId = 0;
        int startIndex = -1;

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (!IsMultiUseMarker(document.Lines[index]))
                continue;

            currentRecipeId++;

            if (currentRecipeId == recipeId)
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
            if (!IsMultiUseMarker(document.Lines[index]))
                continue;

            endIndexExclusive = index;
            break;
        }

        return new MultiUseBlock(
            StartIndex: startIndex,
            EndIndexExclusive: endIndexExclusive);
    }

    private static bool IsMultiUseMarker(IniLine line)
    {
        if (line is IniFlagLine flagLine)
        {
            return flagLine.Name.Equals("MultiUse", StringComparison.OrdinalIgnoreCase)
                || flagLine.Name.Equals("Multiuse", StringComparison.OrdinalIgnoreCase);
        }

        if (line is IniKeyValueLine keyValueLine)
        {
            return keyValueLine.Key.Equals("MultiUse", StringComparison.OrdinalIgnoreCase)
                || keyValueLine.Key.Equals("Multiuse", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static IniKeyValueLine? FindFieldInBlock(
        IniDocument document,
        MultiUseBlock block,
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
        MultiUseBlock block,
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
        MultiUseBlock block,
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
        MultiUseBlock block,
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
        MultiUseBlock block)
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

    private readonly record struct MultiUseBlock(
        int StartIndex,
        int EndIndexExclusive);
}