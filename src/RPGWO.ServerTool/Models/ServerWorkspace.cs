using RPGWO.Formats.Rpgwo;
using System.Collections.Generic;
using System.Linq;

namespace RPGWO.ServerTool.Models;

/// <summary>
/// Represents the currently opened RPGWO server folder and all parsed server files.
/// Editors can use this shared workspace to reference data across files.
/// </summary>
public sealed class ServerWorkspace
{
    public required string ServerFolderPath { get; init; }

    public required IReadOnlyList<ServerFileStatus> FileStatuses { get; init; }

    public ItemIniReadResult? Items { get; init; }

    public MonsterIniReadResult? Monsters { get; init; }

    public SkillIniReadResult? Skills { get; init; }

    public UsageIniReadResult? Usages { get; init; }

    public MultiUseIniReadResult? MultiUses { get; init; }

    public MagicIniReadResult? Magic { get; init; }

    public TreasureIniReadResult? Treasures { get; init; }

    public WorldIniReadResult? World { get; init; }

    public AnimationIniReadResult? Animations { get; init; }

    public ServerFileStatus? GetStatus(SupportedServerFile fileType)
    {
        return FileStatuses.FirstOrDefault(status => status.FileType == fileType);
    }

    public bool HasReadyFile(SupportedServerFile fileType)
    {
        ServerFileStatus? status = GetStatus(fileType);
        return status is not null && status.Exists && status.Loaded && status.IssueCount == 0;
    }
}