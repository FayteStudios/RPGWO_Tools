namespace RPGWO.Formats.Ini;

/// <summary>
/// Loose INI parser intended for legacy RPGWO server files.
/// It is designed to preserve every line, not enforce a strict INI standard.
/// </summary>
public static class IniParser
{
    public static IniParseResult ParseFile(
        string path,
        IniParseOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException("INI file was not found.", path);

        string text = File.ReadAllText(path);
        IniParseResult result = ParseText(text, options);
        result.Document.SourcePath = path;

        return result;
    }

    public static IniParseResult ParseText(
        string text,
        IniParseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        options ??= IniParseOptions.Default;

        var document = new IniDocument
        {
            NewLine = DetectNewLine(text)
        };

        var result = new IniParseResult(document);

        string[] lines = SplitLinesPreserveLogicalLines(text);

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];
            int lineNumber = index + 1;

            document.Lines.Add(ParseLine(line, lineNumber, options, result));
        }

        return result;
    }

    private static IniLine ParseLine(
        string line,
        int lineNumber,
        IniParseOptions options,
        IniParseResult result)
    {
        string trimmedStart = line.TrimStart();

        if (string.IsNullOrWhiteSpace(line))
        {
            return new IniBlankLine
            {
                LineNumber = lineNumber,
                OriginalText = line
            };
        }

        if (options.AllowSemicolonComments && trimmedStart.StartsWith(';'))
        {
            return new IniCommentLine
            {
                LineNumber = lineNumber,
                OriginalText = line,
                Marker = ';',
                Text = trimmedStart[1..]
            };
        }

        if (options.AllowApostropheComments && trimmedStart.StartsWith('\''))
        {
            return new IniCommentLine
            {
                LineNumber = lineNumber,
                OriginalText = line,
                Marker = '\'',
                Text = trimmedStart[1..]
            };
        }

        if (options.AllowSections && IsSectionLine(trimmedStart, out string sectionName))
        {
            return new IniSectionLine
            {
                LineNumber = lineNumber,
                OriginalText = line,
                Name = sectionName
            };
        }

        int separatorIndex = line.IndexOf('=');
        if (separatorIndex >= 0)
        {
            string rawKey = line[..separatorIndex];
            string rawValue = line[(separatorIndex + 1)..];

            string key = options.TrimKeys ? rawKey.Trim() : rawKey;
            string value = options.TrimValues ? rawValue.Trim() : rawValue;

            if (string.IsNullOrWhiteSpace(key))
            {
                result.Warnings.Add($"Line {lineNumber}: key/value line has an empty key.");
            }

            return new IniKeyValueLine
            {
                LineNumber = lineNumber,
                OriginalText = line,
                Key = key,
                Value = value,
                Separator = "=",
                IsModified = false
            };
        }

        if (options.PreserveUnknownLines)
        {
            return new IniFlagLine
            {
                LineNumber = lineNumber,
                OriginalText = line,
                Name = line.Trim()
            };
        }

        throw new FormatException($"Line {lineNumber} is not a valid INI line: {line}");
    }

    private static bool IsSectionLine(string line, out string sectionName)
    {
        sectionName = "";

        if (!line.StartsWith('[') || !line.EndsWith(']'))
            return false;

        if (line.Length < 3)
            return false;

        sectionName = line[1..^1].Trim();
        return sectionName.Length > 0;
    }

    private static string DetectNewLine(string text)
    {
        int crlfIndex = text.IndexOf("\r\n", StringComparison.Ordinal);
        int lfIndex = text.IndexOf('\n');
        int crIndex = text.IndexOf('\r');

        if (crlfIndex >= 0)
            return "\r\n";

        if (lfIndex >= 0)
            return "\n";

        if (crIndex >= 0)
            return "\r";

        return Environment.NewLine;
    }

    private static string[] SplitLinesPreserveLogicalLines(string text)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        if (text.EndsWith('\n'))
            text = text[..^1];

        return text.Split('\n');
    }
}