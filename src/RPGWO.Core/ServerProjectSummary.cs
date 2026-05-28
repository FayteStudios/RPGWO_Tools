namespace RPGWO.Core;

/// <summary>
/// Lightweight summary of a scanned RPGWO server project.
/// Useful for CLI output and future UI dashboard cards.
/// </summary>
public sealed class ServerProjectSummary
{
    public ServerProjectSummary(ServerProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        RootPath = project.RootPath;
        TotalFiles = project.Files.Count;

        DefinitionFileCount = project.DefinitionFiles.Count();
        RuntimeDataFileCount = project.RuntimeDataFiles.Count();
        SpriteSheetCount = project.SpriteSheets.Count();

        UnknownFileCount = project.Files.Count(file => file.Kind == ServerFileKind.Unknown);

        HasWorldIni = project.WorldIni is not null;
        HasItemIni = project.ItemIni is not null;
        HasMonsterIni = project.MonsterIni is not null;
        HasMagicIni = project.MagicIni is not null;
        HasSkillIni = project.SkillIni is not null;
        HasItemUseIni = project.ItemUseIni is not null;
        HasMultiUseIni = project.MultiUseIni is not null;
        HasTreasureIni = project.TreasureIni is not null;
        HasAnimationIni = project.AnimationIni is not null;
    }

    public string RootPath { get; }

    public int TotalFiles { get; }
    public int DefinitionFileCount { get; }
    public int RuntimeDataFileCount { get; }
    public int SpriteSheetCount { get; }
    public int UnknownFileCount { get; }

    public bool HasWorldIni { get; }
    public bool HasItemIni { get; }
    public bool HasMonsterIni { get; }
    public bool HasMagicIni { get; }
    public bool HasSkillIni { get; }
    public bool HasItemUseIni { get; }
    public bool HasMultiUseIni { get; }
    public bool HasTreasureIni { get; }
    public bool HasAnimationIni { get; }
}