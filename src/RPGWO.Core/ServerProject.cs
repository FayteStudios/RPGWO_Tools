namespace RPGWO.Core;

/// <summary>
/// Represents an RPGWO server folder and the files detected inside it.
/// This class should not parse files directly. It only stores project-level metadata.
/// </summary>
public sealed class ServerProject
{
    public ServerProject(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));

        RootPath = Path.GetFullPath(rootPath);
    }

    /// <summary>
    /// Absolute path to the root server folder.
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// Files and folders detected in the server project.
    /// </summary>
    public List<ServerFileReference> Files { get; } = new();

    /// <summary>
    /// Returns all files of a specific kind.
    /// </summary>
    public IEnumerable<ServerFileReference> GetFilesByKind(ServerFileKind kind)
    {
        return Files.Where(file => file.Kind == kind);
    }

    /// <summary>
    /// Returns the first file of a specific kind, or null if not found.
    /// Useful for singleton files like item.ini, monster.ini, world.ini, etc.
    /// </summary>
    public ServerFileReference? GetFirstFileByKind(ServerFileKind kind)
    {
        return Files.FirstOrDefault(file => file.Kind == kind);
    }

    public ServerFileReference? WorldIni => GetFirstFileByKind(ServerFileKind.WorldIni);
    public ServerFileReference? ItemIni => GetFirstFileByKind(ServerFileKind.ItemIni);
    public ServerFileReference? MonsterIni => GetFirstFileByKind(ServerFileKind.MonsterIni);
    public ServerFileReference? MagicIni => GetFirstFileByKind(ServerFileKind.MagicIni);
    public ServerFileReference? SkillIni => GetFirstFileByKind(ServerFileKind.SkillIni);
    public ServerFileReference? ItemUseIni => GetFirstFileByKind(ServerFileKind.ItemUseIni);
    public ServerFileReference? MultiUseIni => GetFirstFileByKind(ServerFileKind.MultiUseIni);
    public ServerFileReference? TreasureIni => GetFirstFileByKind(ServerFileKind.TreasureIni);
    public ServerFileReference? AnimationIni => GetFirstFileByKind(ServerFileKind.AnimationIni);
    public ServerFileReference? WaterSideIni => GetFirstFileByKind(ServerFileKind.WaterSideIni);
    public ServerFileReference? UnderSideIni => GetFirstFileByKind(ServerFileKind.UnderSideIni);

    /// <summary>
    /// All known editable/configurable INI files.
    /// </summary>
    public IEnumerable<ServerFileReference> DefinitionFiles =>
        Files.Where(file => file.IsKnownDefinitionFile);

    /// <summary>
    /// All detected runtime .dat files.
    /// </summary>
    public IEnumerable<ServerFileReference> RuntimeDataFiles =>
        Files.Where(file => file.IsRuntimeDataFile);

    /// <summary>
    /// All detected sprite sheets.
    /// </summary>
    public IEnumerable<ServerFileReference> SpriteSheets =>
        Files.Where(file => file.IsSpriteSheet);

    /// <summary>
    /// Adds a detected file to the project.
    /// Intended to be called by the folder scanner in RPGWO.Formats later.
    /// </summary>
    public void AddFile(ServerFileReference file)
    {
        ArgumentNullException.ThrowIfNull(file);
        Files.Add(file);
    }

    /// <summary>
    /// Adds multiple detected files to the project.
    /// </summary>
    public void AddFiles(IEnumerable<ServerFileReference> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        foreach (var file in files)
            AddFile(file);
    }

    public override string ToString()
    {
        return $"{RootPath} ({Files.Count} files)";
    }
}