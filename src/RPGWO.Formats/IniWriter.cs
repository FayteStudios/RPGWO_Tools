using System.Text;

namespace RPGWO.Formats.Ini;

/// <summary>
/// Writes parsed INI documents back to text.
/// By default this attempts to preserve original lines unless a key/value line was modified.
/// </summary>
public static class IniWriter
{
    public static void WriteFile(
        string path,
        IniDocument document,
        bool preserveUnmodifiedOriginalText = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);

        string text = WriteToString(document, preserveUnmodifiedOriginalText);
        File.WriteAllText(path, text);
    }

    public static string WriteToString(
        IniDocument document,
        bool preserveUnmodifiedOriginalText = true)
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();

        for (int i = 0; i < document.Lines.Count; i++)
        {
            IniLine line = document.Lines[i];
            string output = WriteLine(line, preserveUnmodifiedOriginalText);

            builder.Append(output);

            if (i < document.Lines.Count - 1)
                builder.Append(document.NewLine);
        }

        return builder.ToString();
    }

    private static string WriteLine(
        IniLine line,
        bool preserveUnmodifiedOriginalText)
    {
        return line switch
        {
            IniKeyValueLine keyValue => WriteKeyValueLine(keyValue, preserveUnmodifiedOriginalText),
            _ => line.OriginalText
        };
    }

    private static string WriteKeyValueLine(
        IniKeyValueLine line,
        bool preserveUnmodifiedOriginalText)
    {
        if (preserveUnmodifiedOriginalText && !line.IsModified)
            return line.OriginalText;

        return $"{line.Key}{line.Separator}{line.Value}";
    }
}