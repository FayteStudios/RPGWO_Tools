namespace RPGWO.ServerTool.Models;

/// <summary>
/// Result from saving and re-validating a server INI file.
/// </summary>
public sealed class SaveValidationResult
{
    public required bool Success { get; init; }

    public required string FilePath { get; init; }

    public string? BackupPath { get; init; }

    public int UnknownCount { get; init; }

    public int IssueCount { get; init; }

    public string Message
    {
        get
        {
            if (!Success)
                return "Save failed.";

            if (IssueCount > 0)
                return $"Saved with {IssueCount} issue(s).";

            if (UnknownCount > 0)
                return $"Saved with {UnknownCount} unknown field(s).";

            return "Saved and validated successfully.";
        }
    }

    public string? ErrorMessage { get; init; }
}