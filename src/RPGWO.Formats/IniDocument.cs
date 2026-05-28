namespace RPGWO.Formats.Ini;

/// <summary>
/// Represents a parsed INI-style document.
/// This is intentionally generic and preserves every line.
/// Higher-level RPGWO parsers will interpret known keys later.
/// </summary>
public sealed class IniDocument
{
    public string? SourcePath { get; set; }

    /// <summary>
    /// Newline sequence detected from the source file.
    /// Usually "\r\n" on Windows files or "\n" on Unix-style files.
    /// </summary>
    public string NewLine { get; set; } = Environment.NewLine;

    public List<IniLine> Lines { get; } = new();

    public IEnumerable<IniKeyValueLine> KeyValueLines =>
        Lines.OfType<IniKeyValueLine>();

    public IEnumerable<IniCommentLine> CommentLines =>
        Lines.OfType<IniCommentLine>();

    public IEnumerable<IniBlankLine> BlankLines =>
        Lines.OfType<IniBlankLine>();

    public IEnumerable<IniSectionLine> SectionLines =>
        Lines.OfType<IniSectionLine>();

    public IEnumerable<IniRawLine> RawLines =>
        Lines.OfType<IniRawLine>();

    public int LineCount => Lines.Count;

    public int KeyValueCount => KeyValueLines.Count();

    public int CommentCount => CommentLines.Count();

    public int BlankCount => BlankLines.Count();

    public int SectionCount => SectionLines.Count();

    public int RawCount => RawLines.Count();

    public IReadOnlyCollection<string> UniqueKeys =>
        KeyValueLines
            .Select(line => line.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IEnumerable<IniKeyValueLine> GetValues(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return KeyValueLines.Where(line =>
            string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase));
    }
    public IEnumerable<IniFlagLine> FlagLines =>
    Lines.OfType<IniFlagLine>();

    public int FlagCount => FlagLines.Count();
    public IniKeyValueLine? GetFirstValue(string key)
    {
        return GetValues(key).FirstOrDefault();
    }
}