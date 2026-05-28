using RPGWO.Core;

namespace RPGWO.Formats.Detection;

/// <summary>
/// Classifies files and folders commonly found in RPGWO server projects.
/// </summary>
public static class ServerFileClassifier
{
    private static readonly Dictionary<string, ServerFileKind> KnownFiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["world.ini"] = ServerFileKind.WorldIni,
            ["item.ini"] = ServerFileKind.ItemIni,
            ["monster.ini"] = ServerFileKind.MonsterIni,
            ["magic.ini"] = ServerFileKind.MagicIni,
            ["skill.ini"] = ServerFileKind.SkillIni,
            ["itemuse.ini"] = ServerFileKind.ItemUseIni,
            ["multiuse.ini"] = ServerFileKind.MultiUseIni,
            ["treasure.ini"] = ServerFileKind.TreasureIni,
            ["animation.ini"] = ServerFileKind.AnimationIni,
            ["waterside.ini"] = ServerFileKind.WaterSideIni,
            ["underside.ini"] = ServerFileKind.UnderSideIni,
        };

    public static ServerFileKind ClassifyPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Directory.Exists(path))
            return ServerFileKind.Folder;

        string fileName = Path.GetFileName(path);

        if (KnownFiles.TryGetValue(fileName, out ServerFileKind knownKind))
            return knownKind;

        string extension = Path.GetExtension(path);

        return extension.ToLowerInvariant() switch
        {
            ".dat" => ServerFileKind.DataFile,
            ".bmp" => ServerFileKind.SpriteSheet,
            ".txt" => ServerFileKind.TextFile,
            ".log" => ServerFileKind.LogFile,
            ".exe" => ServerFileKind.Executable,
            ".dll" => ServerFileKind.Library,
            ".ocx" => ServerFileKind.Library,
            _ => ServerFileKind.Unknown
        };
    }

    public static bool IsKnownRpgwoDefinitionFile(string path)
    {
        ServerFileKind kind = ClassifyPath(path);

        return kind is ServerFileKind.WorldIni
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
    }
}