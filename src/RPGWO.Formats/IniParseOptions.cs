namespace RPGWO.Formats.Ini;

/// <summary>
/// Options for the loose INI parser.
/// Defaults are designed for RPGWO server files.
/// </summary>
public sealed class IniParseOptions
{
    public static IniParseOptions Default { get; } = new();

    /// <summary>
    /// Treat lines starting with ; as comments.
    /// </summary>
    public bool AllowSemicolonComments { get; init; } = true;

    /// <summary>
    /// Treat lines starting with ' as comments.
    /// Older VB-related configs often use apostrophe comments.
    /// </summary>
    public bool AllowApostropheComments { get; init; } = true;

    /// <summary>
    /// Treat [Name] lines as section headers.
    /// </summary>
    public bool AllowSections { get; init; } = true;

    /// <summary>
    /// Trim whitespace around keys.
    /// Example: " Name = Sword" becomes key "Name".
    /// </summary>
    public bool TrimKeys { get; init; } = true;

    /// <summary>
    /// Trim whitespace around values.
    /// Example: "Name = Sword " becomes value "Sword".
    /// </summary>
    public bool TrimValues { get; init; } = false;

    /// <summary>
    /// If true, lines without a key/value separator become raw preserved lines.
    /// If false, parsing throws.
    /// </summary>
    public bool PreserveUnknownLines { get; init; } = true;
}