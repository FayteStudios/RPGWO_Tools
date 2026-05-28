using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Scans a server folder for supported RPGWO INI files and summarizes parse status.
/// This service is intentionally UI-agnostic so it can later be tested or reused.
/// </summary>
public sealed class ServerFolderScanner
{
    public IReadOnlyList<ServerFileStatus> Scan(string serverFolderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverFolderPath);

        var statuses = new List<ServerFileStatus>
        {
            ScanItems(serverFolderPath),
            ScanMonsters(serverFolderPath),
            ScanSkills(serverFolderPath),
            ScanUsages(serverFolderPath),
            ScanMultiUses(serverFolderPath),
            ScanMagic(serverFolderPath),
            ScanTreasures(serverFolderPath),
            ScanWorld(serverFolderPath),
            ScanAnimations(serverFolderPath)
        };

        return statuses;
    }

    private static ServerFileStatus ScanItems(string folder)
    {
        string path = Path.Combine(folder, "item.ini");

        return SafeScan(
            SupportedServerFile.Items,
            "Items",
            "item.ini",
            path,
            () =>
            {
                var result = ItemIniReader.ReadFile(path);

                return new ScanCounts(
                    EntryCount: result.Items.Count,
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static ServerFileStatus ScanMonsters(string folder)
    {
        string path = Path.Combine(folder, "monster.ini");

        return SafeScan(
            SupportedServerFile.Monsters,
            "Monsters",
            "monster.ini",
            path,
            () =>
            {
                var result = MonsterIniReader.ReadFile(path);

                return new ScanCounts(
                    EntryCount: result.Monsters.Count,
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static ServerFileStatus ScanSkills(string folder)
    {
        string path = Path.Combine(folder, "skill.ini");

        return SafeScan(
            SupportedServerFile.Skills,
            "Skills",
            "skill.ini",
            path,
            () =>
            {
                var result = SkillIniReader.ReadFile(path);

                return new ScanCounts(
                    EntryCount: result.Skills.Count,
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static ServerFileStatus ScanUsages(string folder)
    {
        string path = Path.Combine(folder, "itemuse.ini");

        return SafeScan(
            SupportedServerFile.Usages,
            "Item Uses",
            "itemuse.ini",
            path,
            () =>
            {
                var result = UsageIniReader.ReadFile(path);

                return new ScanCounts(
                    EntryCount: result.Usages.Count,
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static ServerFileStatus ScanMultiUses(string folder)
    {
        string path = Path.Combine(folder, "multiuse.ini");

        return SafeScan(
            SupportedServerFile.MultiUses,
            "Multi Uses",
            "multiuse.ini",
            path,
            () =>
            {
                var result = MultiUseIniReader.ReadFile(path);

                return new ScanCounts(
                    EntryCount: result.Recipes.Count,
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static ServerFileStatus ScanMagic(string folder)
    {
        string path = Path.Combine(folder, "magic.ini");

        return SafeScan(
            SupportedServerFile.Magic,
            "Magic",
            "magic.ini",
            path,
            () =>
            {
                var result = MagicIniReader.ReadFile(path);

                return new ScanCounts(
                    EntryCount: result.Spells.Count,
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static ServerFileStatus ScanTreasures(string folder)
    {
        string path = Path.Combine(folder, "treasure.ini");

        return SafeScan(
            SupportedServerFile.Treasures,
            "Treasures",
            "treasure.ini",
            path,
            () =>
            {
                var result = TreasureIniReader.ReadFile(path);

                return new ScanCounts(
                    EntryCount: result.Treasures.Count,
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static ServerFileStatus ScanWorld(string folder)
    {
        string path = Path.Combine(folder, "world.ini");

        return SafeScan(
            SupportedServerFile.World,
            "World",
            "world.ini",
            path,
            () =>
            {
                var result = WorldIniReader.ReadFile(path);

                return new ScanCounts(
                    EntryCount: result.World.Settings.Count + result.World.Flags.Count,
                    UnknownCount: result.UnknownFieldCount + result.UnknownFlagCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static ServerFileStatus ScanAnimations(string folder)
    {
        string path = Path.Combine(folder, "animation.ini");

        return SafeScan(
            SupportedServerFile.Animations,
            "Animations",
            "animation.ini",
            path,
            () =>
            {
                var result = AnimationIniReader.ReadFile(path);

                return new ScanCounts(
                    EntryCount: result.Animations.Count,
                    UnknownCount: result.UnknownFieldCount,
                    IssueCount: result.Issues.Count);
            });
    }

    private static ServerFileStatus SafeScan(
        SupportedServerFile fileType,
        string displayName,
        string fileName,
        string filePath,
        Func<ScanCounts> scan)
    {
        if (!File.Exists(filePath))
        {
            return new ServerFileStatus
            {
                FileType = fileType,
                DisplayName = displayName,
                FileName = fileName,
                FilePath = filePath,
                Exists = false,
                Loaded = false,
                EntryCount = 0,
                UnknownCount = 0,
                IssueCount = 0
            };
        }

        try
        {
            ScanCounts counts = scan();

            return new ServerFileStatus
            {
                FileType = fileType,
                DisplayName = displayName,
                FileName = fileName,
                FilePath = filePath,
                Exists = true,
                Loaded = true,
                EntryCount = counts.EntryCount,
                UnknownCount = counts.UnknownCount,
                IssueCount = counts.IssueCount
            };
        }
        catch (Exception ex)
        {
            return new ServerFileStatus
            {
                FileType = fileType,
                DisplayName = displayName,
                FileName = fileName,
                FilePath = filePath,
                Exists = true,
                Loaded = false,
                EntryCount = 0,
                UnknownCount = 0,
                IssueCount = 1,
                ErrorMessage = ex.Message
            };
        }
    }

    private readonly record struct ScanCounts(
        int EntryCount,
        int UnknownCount,
        int IssueCount);
}