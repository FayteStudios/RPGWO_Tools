using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Loads a full server workspace from a selected RPGWO server folder.
/// The scanner provides status rows. The workspace stores parsed read results for cross-file editors.
/// </summary>
public sealed class ServerWorkspaceService
{
    private readonly ServerFolderScanner _scanner = new();

    public ServerWorkspace Load(string serverFolderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverFolderPath);

        IReadOnlyList<ServerFileStatus> statuses = _scanner.Scan(serverFolderPath);

        return new ServerWorkspace
        {
            ServerFolderPath = serverFolderPath,
            FileStatuses = statuses,

            Items = SafeLoad(
                Path.Combine(serverFolderPath, "item.ini"),
                ItemIniReader.ReadFile),

            Monsters = SafeLoad(
                Path.Combine(serverFolderPath, "monster.ini"),
                MonsterIniReader.ReadFile),

            Skills = SafeLoad(
                Path.Combine(serverFolderPath, "skill.ini"),
                SkillIniReader.ReadFile),

            Usages = SafeLoad(
                Path.Combine(serverFolderPath, "itemuse.ini"),
                UsageIniReader.ReadFile),

            MultiUses = SafeLoad(
                Path.Combine(serverFolderPath, "multiuse.ini"),
                MultiUseIniReader.ReadFile),

            Magic = SafeLoad(
                Path.Combine(serverFolderPath, "magic.ini"),
                MagicIniReader.ReadFile),

            Treasures = SafeLoad(
                Path.Combine(serverFolderPath, "treasure.ini"),
                TreasureIniReader.ReadFile),

            World = SafeLoad(
                Path.Combine(serverFolderPath, "world.ini"),
                WorldIniReader.ReadFile),

            Animations = SafeLoad(
                Path.Combine(serverFolderPath, "animation.ini"),
                AnimationIniReader.ReadFile)
        };
    }

    private static T? SafeLoad<T>(
        string filePath,
        Func<string, T> load)
        where T : class
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            return load(filePath);
        }
        catch
        {
            return null;
        }
    }
}