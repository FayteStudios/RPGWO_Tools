using System;
using System.IO;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Creates timestamped backups before modifying server INI files.
/// </summary>
public sealed class BackupManager
{
    public string CreateBackup(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Cannot create backup because the source file does not exist.", filePath);

        string? directory = Path.GetDirectoryName(filePath);

        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Could not determine file directory.");

        string backupDirectory = Path.Combine(directory, "Backups");

        Directory.CreateDirectory(backupDirectory);

        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string backupPath = Path.Combine(
            backupDirectory,
            $"{fileNameWithoutExtension}_{timestamp}{extension}");

        File.Copy(filePath, backupPath, overwrite: false);

        return backupPath;
    }
}