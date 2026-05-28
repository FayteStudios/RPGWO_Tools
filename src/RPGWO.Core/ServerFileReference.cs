namespace RPGWO.Core;

/// <summary>
/// Represents a file or folder detected inside an RPGWO server project.
/// This is metadata only. The file contents are handled by RPGWO.Formats later.
/// </summary>
public sealed class ServerFileReference
{
    public ServerFileReference(
        string fullPath,
        string relativePath,
        ServerFileKind kind)
    {
        FullPath = fullPath;
        RelativePath = relativePath;
        Kind = kind;
        FileName = Path.GetFileName(fullPath);
        Extension = Path.GetExtension(fullPath);
    }

    /// <summary>
    /// Absolute path on disk.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Path relative to the selected server root folder.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// File name only, such as item.ini or monster.ini.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// File extension, including the dot. Example: .ini, .dat, .bmp.
    /// </summary>
    public string Extension { get; }

    /// <summary>
    /// Detected RPGWO file kind.
    /// </summary>
    public ServerFileKind Kind { get; }

    /// <summary>
    /// True when this reference points to a known RPGWO definition INI file.
    /// </summary>
    public bool IsKnownDefinitionFile =>
        Kind is ServerFileKind.WorldIni
            or ServerFileKind.ItemIni
            or ServerFileKind.MonsterIni
            or ServerFileKind.MagicIni
            or ServerFileKind.SkillIni
            or ServerFileKind.ItemUseIni
            or ServerFileKind.MultiUseIni
            or ServerFileKind.TreasureIni
            or ServerFileKind.AnimationIni
            or ServerFileKind.WaterSideIni
            or ServerFileKind.UnderSideIni;

    /// <summary>
    /// True when the file is probably runtime-generated server state.
    /// </summary>
    public bool IsRuntimeDataFile => Kind == ServerFileKind.DataFile;

    /// <summary>
    /// True when the file is a sprite sheet image.
    /// </summary>
    public bool IsSpriteSheet => Kind == ServerFileKind.SpriteSheet;

    public override string ToString()
    {
        return $"{RelativePath} ({Kind})";
    }
}