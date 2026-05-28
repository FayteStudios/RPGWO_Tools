namespace RPGWO.Data.Common;

/// <summary>
/// Represents a key/value line that was found in a legacy RPGWO file
/// but is not currently mapped to a strongly typed property.
/// </summary>
public sealed class UnknownField
{
    public UnknownField(
        string key,
        string value,
        int lineNumber,
        string originalText)
    {
        Key = key;
        Value = value;
        LineNumber = lineNumber;
        OriginalText = originalText;
    }

    public string Key { get; }

    public string Value { get; }

    public int LineNumber { get; }

    public string OriginalText { get; }

    public override string ToString()
    {
        return $"{Key}={Value} at line {LineNumber}";
    }
}