using RPGWO.Core;

namespace RPGWO.Formats.Detection;

/// <summary>
/// Scans a server folder and creates a ServerProject file inventory.
/// This scanner does not parse file contents; it only detects files and classifies them.
/// </summary>
public static class ServerFolderScanner
{
    public static ServerProject Scan(
        string rootPath,
        bool includeSubdirectories = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Server folder was not found: {rootPath}");

        string normalizedRoot = Path.GetFullPath(rootPath);
        var project = new ServerProject(normalizedRoot);

        SearchOption searchOption = includeSubdirectories
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        foreach (string path in Directory.EnumerateFileSystemEntries(normalizedRoot, "*", searchOption))
        {
            ServerFileKind kind = ServerFileClassifier.ClassifyPath(path);
            string relativePath = Path.GetRelativePath(normalizedRoot, path);

            project.AddFile(new ServerFileReference(
                fullPath: path,
                relativePath: relativePath,
                kind: kind));
        }

        return project;
    }
}