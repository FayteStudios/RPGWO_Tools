namespace RPGWO.Core;

/// <summary>
/// Broad classification for files found in an RPGWO server folder.
/// This does not mean the file has been parsed yet; it only identifies what kind of file it appears to be.
/// </summary>
public enum ServerFileKind
{
    Unknown = 0,

    // Primary editable INI files
    WorldIni,
    ItemIni,
    MonsterIni,
    MagicIni,
    SkillIni,
    ItemUseIni,
    MultiUseIni,
    TreasureIni,
    AnimationIni,

    // Supporting INI files discovered from the old server executable
    WaterSideIni,
    UnderSideIni,

    // Runtime / generated server files
    DataFile,

    // Asset files
    SpriteSheet,

    // Human-readable support files
    TextFile,
    LogFile,

    // Executables / binaries
    Executable,
    Library,

    // Folders
    Folder
}