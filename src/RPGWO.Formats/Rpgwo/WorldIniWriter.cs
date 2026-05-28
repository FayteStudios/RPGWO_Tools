using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Safely updates existing RPGWO world.ini documents while preserving original formatting,
/// comments, unknown fields, flags, and ordering.
///
/// world.ini is a single global key/value + bare-flag settings file.
/// This writer updates the last matching key by default because world.ini can contain
/// valid repeated keys such as Terrain, StarterSpell, and FactionTroop.
/// </summary>
public static class WorldIniWriter
{
    public static bool WriteUpdatedSettingToFile(
        string inputPath,
        string outputPath,
        string key,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        SetWorldSetting(
            parseResult.Document,
            key,
            value);

        IniWriter.WriteFile(outputPath, parseResult.Document);
        return true;
    }

    public static bool SetWorldSetting(
        IniDocument document,
        string key,
        string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        IniKeyValueLine? existingLine = FindLastSetting(document, key);

        if (existingLine is not null)
        {
            existingLine.Value = value;
            existingLine.IsModified = true;
            return true;
        }

        InsertSettingAtEnd(document, key, value);
        return true;
    }

    public static bool RemoveWorldSetting(
        IniDocument document,
        string key)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        bool removedAny = false;

        for (int index = document.Lines.Count - 1; index >= 0; index--)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (!keyValueLine.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            document.Lines.RemoveAt(index);
            removedAny = true;
        }

        return removedAny;
    }

    public static bool SetWorldFlag(
        IniDocument document,
        string flagName,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        List<int> matchingFlagIndexes = FindFlagIndexes(document, flagName);

        if (enabled)
        {
            if (matchingFlagIndexes.Count > 0)
                return true;

            InsertFlagAtEnd(document, flagName);
            return true;
        }

        for (int i = matchingFlagIndexes.Count - 1; i >= 0; i--)
            document.Lines.RemoveAt(matchingFlagIndexes[i]);

        return true;
    }

    public static bool ContainsSetting(
        IniDocument document,
        string key)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return FindLastSetting(document, key) is not null;
    }

    public static bool ContainsFlag(
        IniDocument document,
        string flagName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        return FindFlagIndexes(document, flagName).Count > 0;
    }

    private static IniKeyValueLine? FindLastSetting(
        IniDocument document,
        string key)
    {
        for (int index = document.Lines.Count - 1; index >= 0; index--)
        {
            if (document.Lines[index] is not IniKeyValueLine keyValueLine)
                continue;

            if (keyValueLine.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                return keyValueLine;
        }

        return null;
    }

    private static List<int> FindFlagIndexes(
        IniDocument document,
        string flagName)
    {
        var indexes = new List<int>();

        for (int index = 0; index < document.Lines.Count; index++)
        {
            if (document.Lines[index] is not IniFlagLine flagLine)
                continue;

            if (flagLine.Name.Equals(flagName, StringComparison.OrdinalIgnoreCase))
                indexes.Add(index);
        }

        return indexes;
    }

    private static void InsertSettingAtEnd(
        IniDocument document,
        string key,
        string value)
    {
        int insertIndex = FindInsertionIndex(document);

        document.Lines.Insert(
            insertIndex,
            new IniKeyValueLine
            {
                LineNumber = 0,
                OriginalText = $"{key}={value}",
                Key = key,
                Value = value,
                Separator = "=",
                IsModified = false
            });
    }

    private static void InsertFlagAtEnd(
        IniDocument document,
        string flagName)
    {
        int insertIndex = FindInsertionIndex(document);

        document.Lines.Insert(
            insertIndex,
            new IniFlagLine
            {
                LineNumber = 0,
                OriginalText = flagName,
                Name = flagName
            });
    }

    private static int FindInsertionIndex(IniDocument document)
    {
        int insertIndex = document.Lines.Count;

        for (int index = document.Lines.Count - 1; index >= 0; index--)
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
}