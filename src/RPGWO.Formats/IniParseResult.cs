namespace RPGWO.Formats.Ini;

/// <summary>
/// Result from parsing an INI document.
/// Contains the parsed document plus non-fatal warnings.
/// </summary>
public sealed class IniParseResult
{
    public IniParseResult(IniDocument document)
    {
        Document = document;
    }

    public IniDocument Document { get; }

    public List<string> Warnings { get; } = new();

    public bool HasWarnings => Warnings.Count > 0;
}