using RPGWO.ServerTool.Models;
using System;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Coordinates backup + save + validation operations for server files.
/// The caller supplies the actual save and validation behavior.
/// </summary>
public sealed class SaveOperationService
{
    private readonly BackupManager _backupManager = new();

    public SaveValidationResult SaveWithBackup(
        string filePath,
        Action saveAction,
        Func<(int UnknownCount, int IssueCount)> validateAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(saveAction);
        ArgumentNullException.ThrowIfNull(validateAction);

        try
        {
            string backupPath = _backupManager.CreateBackup(filePath);

            saveAction();

            (int unknownCount, int issueCount) = validateAction();

            return new SaveValidationResult
            {
                Success = true,
                FilePath = filePath,
                BackupPath = backupPath,
                UnknownCount = unknownCount,
                IssueCount = issueCount
            };
        }
        catch (Exception ex)
        {
            return new SaveValidationResult
            {
                Success = false,
                FilePath = filePath,
                ErrorMessage = ex.Message
            };
        }
    }
}