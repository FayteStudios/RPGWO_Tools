namespace RPGWO.Formats.Ini;

/// <summary>
/// Base type for every parsed line in an INI-style file.
/// The parser is intentionally loss-tolerant: every source line becomes an IniLine.
/// </summary>
public abstract class IniLine
{
    public int LineNumber { get; init; }

    /// <summary>
    /// Exact original text for this line, excluding the newline characters.
    /// </summary>
    public string OriginalText { get; init; } = "";
}

public sealed class IniFlagLine : IniLine
{
    public string Name { get; init; } = "";
}
/// <summary>
/// Blank or whitespace-only line.
/// </summary>
public sealed class IniBlankLine : IniLine
{
}

/// <summary>
/// Comment line. RPGWO-era files may use semicolon or apostrophe comments.
/// </summary>
public sealed class IniCommentLine : IniLine
{
    public string Text { get; init; } = "";
    public char Marker { get; init; }
}

/// <summary>
/// Standard section header line, such as [Items].
/// RPGWO files may not always use sections, but the parser supports them safely.
/// </summary>
public sealed class IniSectionLine : IniLine
{
    public string Name { get; init; } = "";
}

/// <summary>
/// Key/value line, usually Key=Value.
/// </summary>
public sealed class IniKeyValueLine : IniLine
{
    public string Key { get; init; } = "";
    public string Value { get; set; } = "";

    /// <summary>
    /// Separator used between key and value. Usually "=".
    /// Kept as a property in case we later support ":" or other legacy separators.
    /// </summary>
    public string Separator { get; init; } = "=";

    /// <summary>
    /// True if Value has been changed after parsing.
    /// Writers can use this to decide whether to preserve OriginalText exactly.
    /// </summary>
    public bool IsModified { get; set; }
}

/// <summary>
/// A line the parser did not classify as blank/comment/section/key-value.
/// These are preserved so old or unknown server-specific syntax is not destroyed.
/// </summary>
public sealed class IniRawLine : IniLine
{
    public string Reason { get; init; } = "";
}