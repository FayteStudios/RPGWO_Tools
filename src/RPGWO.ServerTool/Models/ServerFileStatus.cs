namespace RPGWO.ServerTool.Models;

/// <summary>
/// Dashboard status for one supported server INI file.
/// </summary>
public sealed class ServerFileStatus
{
    public required SupportedServerFile FileType { get; init; }

    public required string DisplayName { get; init; }

    public required string FileName { get; init; }

    public required string FilePath { get; init; }

    public bool Exists { get; init; }

    public bool Loaded { get; init; }

    public int EntryCount { get; init; }

    public int UnknownCount { get; init; }

    public int IssueCount { get; init; }

    public string StatusText
    {
        get
        {
            if (!Exists)
                return "Missing";

            if (!Loaded)
                return "Failed";

            if (IssueCount > 0)
                return "Issues";

            if (UnknownCount > 0)
                return "Unknowns";

            return "Ready";
        }
    }

    public string Summary
    {
        get
        {
            if (!Exists)
                return "File not found.";

            if (!Loaded)
                return "File exists, but could not be loaded.";

            return $"Entries: {EntryCount}, Unknowns: {UnknownCount}, Issues: {IssueCount}";
        }
    }

    public string? ErrorMessage { get; init; }
}