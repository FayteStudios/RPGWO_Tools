namespace RPGWO.Data.Common;

/// <summary>
/// Represents a non-fatal issue encountered while converting parsed INI lines
/// into typed RPGWO definitions.
/// </summary>
public sealed class DefinitionParseIssue
{
    public DefinitionParseIssue(
        string message,
        int? lineNumber = null,
        string? key = null,
        string? value = null)
    {
        Message = message;
        LineNumber = lineNumber;
        Key = key;
        Value = value;
    }

    public string Message { get; }

    public int? LineNumber { get; }

    public string? Key { get; }

    public string? Value { get; }

    public override string ToString()
    {
        string location = LineNumber is null
            ? ""
            : $"Line {LineNumber}: ";

        string keyInfo = string.IsNullOrWhiteSpace(Key)
            ? ""
            : $" [{Key}={Value}]";

        return $"{location}{Message}{keyInfo}";
    }
}