using RPGWO.Formats.Rpgwo;
using RPGWO.ServerTool.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Exports supported RPGWO INI files to CSV for user review.
/// </summary>
public sealed class CsvExportService
{
    public void Export(
        SupportedServerFile fileType,
        string inputPath,
        string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        switch (fileType)
        {
            case SupportedServerFile.Items:
                ExportItems(inputPath, outputPath);
                break;

            case SupportedServerFile.Monsters:
                ExportMonsters(inputPath, outputPath);
                break;

            case SupportedServerFile.Skills:
                ExportSkills(inputPath, outputPath);
                break;

            case SupportedServerFile.Usages:
                ExportUsages(inputPath, outputPath);
                break;

            case SupportedServerFile.MultiUses:
                ExportMultiUses(inputPath, outputPath);
                break;

            case SupportedServerFile.Magic:
                ExportMagic(inputPath, outputPath);
                break;

            case SupportedServerFile.Treasures:
                ExportTreasures(inputPath, outputPath);
                break;

            case SupportedServerFile.World:
                ExportWorld(inputPath, outputPath);
                break;

            case SupportedServerFile.Animations:
                ExportAnimations(inputPath, outputPath);
                break;

            default:
                throw new NotSupportedException($"Unsupported file type: {fileType}");
        }
    }

    private static void ExportItems(string inputPath, string outputPath)
    {
        var result = ItemIniReader.ReadFile(inputPath);

        using var writer = CreateWriter(outputPath);

        writer.WriteLine(string.Join(",",
            "Id",
            "Name",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var item in result.Items.OrderBy(item => item.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(item.Id),
                Csv(item.Name),
                Csv(string.Join("|", item.Flags)),
                Csv(item.UnknownFields.Count),
                Csv(item.Issues.Count)));
        }
    }

    private static void ExportMonsters(string inputPath, string outputPath)
    {
        var result = MonsterIniReader.ReadFile(inputPath);

        using var writer = CreateWriter(outputPath);

        writer.WriteLine(string.Join(",",
            "Id",
            "Name",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var monster in result.Monsters.OrderBy(monster => monster.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(monster.Id),
                Csv(monster.Name),
                Csv(string.Join("|", monster.Flags)),
                Csv(monster.UnknownFields.Count),
                Csv(monster.Issues.Count)));
        }
    }

    private static void ExportSkills(string inputPath, string outputPath)
    {
        var result = SkillIniReader.ReadFile(inputPath);

        using var writer = CreateWriter(outputPath);

        writer.WriteLine(string.Join(",",
            "Id",
            "Name",
            "Usable",
            "SkillPoints",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var skill in result.Skills.OrderBy(skill => skill.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(skill.Id),
                Csv(skill.Name),
                Csv(skill.Usable),
                Csv(skill.SkillPoints),
                Csv(string.Join("|", skill.Flags)),
                Csv(skill.UnknownFields.Count),
                Csv(skill.Issues.Count)));
        }
    }

    private static void ExportUsages(string inputPath, string outputPath)
    {
        var result = UsageIniReader.ReadFile(inputPath);

        using var writer = CreateWriter(outputPath);

        writer.WriteLine(string.Join(",",
            "EntryId",
            "ItemTool",
            "ItemFocus",
            "Skill",
            "SkillMin",
            "SkillMax",
            "SuccessItems",
            "FailedItems",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var usage in result.Usages.OrderBy(usage => usage.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(usage.Id),
                Csv(usage.ItemTool),
                Csv(usage.ItemFocus),
                Csv(usage.Skill),
                Csv(usage.SkillMin),
                Csv(usage.SkillMax),
                Csv(string.Join("|", usage.SuccessItems)),
                Csv(string.Join("|", usage.FailedItems)),
                Csv(string.Join("|", usage.Flags)),
                Csv(usage.UnknownFields.Count),
                Csv(usage.Issues.Count)));
        }
    }

    private static void ExportMultiUses(string inputPath, string outputPath)
    {
        var result = MultiUseIniReader.ReadFile(inputPath);

        using var writer = CreateWriter(outputPath);

        writer.WriteLine(string.Join(",",
            "RecipeId",
            "SuccessItem",
            "SuccessItemQuantity",
            "FocusItem",
            "NeedItems",
            "ResultItems",
            "Skill",
            "SkillMin",
            "SkillMax",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var recipe in result.Recipes.OrderBy(recipe => recipe.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(recipe.Id),
                Csv(recipe.SuccessItem),
                Csv(recipe.SuccessItemQuantity),
                Csv(recipe.FocusItem),
                Csv(string.Join("|", recipe.NeedItems)),
                Csv(string.Join("|", recipe.ResultItems)),
                Csv(recipe.Skill),
                Csv(recipe.SkillMin),
                Csv(recipe.SkillMax),
                Csv(string.Join("|", recipe.Flags)),
                Csv(recipe.UnknownFields.Count),
                Csv(recipe.Issues.Count)));
        }
    }

    private static void ExportMagic(string inputPath, string outputPath)
    {
        var result = MagicIniReader.ReadFile(inputPath);

        using var writer = CreateWriter(outputPath);

        writer.WriteLine(string.Join(",",
            "SpellId",
            "Name",
            "Skill",
            "ManaCost",
            "Range",
            "CastTime",
            "Runes",
            "Animations",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var spell in result.Spells.OrderBy(spell => spell.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(spell.Id),
                Csv(spell.Name),
                Csv(spell.Skill),
                Csv(spell.ManaCost),
                Csv(spell.Range),
                Csv(spell.CastTime),
                Csv(string.Join("|", spell.Runes)),
                Csv(string.Join("|", spell.Animations)),
                Csv(string.Join("|", spell.Flags)),
                Csv(spell.UnknownFields.Count),
                Csv(spell.Issues.Count)));
        }
    }

    private static void ExportTreasures(string inputPath, string outputPath)
    {
        var result = TreasureIniReader.ReadFile(inputPath);

        using var writer = CreateWriter(outputPath);

        writer.WriteLine(string.Join(",",
            "TreasureId",
            "Name",
            "TreasureName",
            "Items",
            "SkillIds",
            "SkillLows",
            "SkillHighs",
            "SpellIds",
            "Cost",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var treasure in result.Treasures.OrderBy(treasure => treasure.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(treasure.Id),
                Csv(treasure.Name),
                Csv(treasure.TreasureName),
                Csv(string.Join("|", treasure.Items)),
                Csv(string.Join("|", treasure.SkillIds)),
                Csv(string.Join("|", treasure.SkillLows)),
                Csv(string.Join("|", treasure.SkillHighs)),
                Csv(string.Join("|", treasure.SpellIds)),
                Csv(treasure.Cost),
                Csv(string.Join("|", treasure.Flags)),
                Csv(treasure.UnknownFields.Count),
                Csv(treasure.Issues.Count)));
        }
    }

    private static void ExportWorld(string inputPath, string outputPath)
    {
        var result = WorldIniReader.ReadFile(inputPath);

        using var writer = CreateWriter(outputPath);

        writer.WriteLine(string.Join(",",
            "LineNumber",
            "Type",
            "Key",
            "Value",
            "Known",
            "OriginalText"));

        foreach (var setting in result.World.Settings)
        {
            writer.WriteLine(string.Join(",",
                Csv(setting.LineNumber),
                Csv("Setting"),
                Csv(setting.Key),
                Csv(setting.Value),
                Csv(setting.IsKnown),
                Csv(setting.OriginalText)));
        }

        foreach (string flag in result.World.Flags)
        {
            writer.WriteLine(string.Join(",",
                Csv(""),
                Csv("Flag"),
                Csv(flag),
                Csv(""),
                Csv(WorldIniReader.IsKnownFlag(flag)),
                Csv(flag)));
        }
    }

    private static void ExportAnimations(string inputPath, string outputPath)
    {
        var result = AnimationIniReader.ReadFile(inputPath);

        using var writer = CreateWriter(outputPath);

        writer.WriteLine(string.Join(",",
            "AnimationId",
            "Name",
            "Rotational",
            "Frames",
            "FrameSizes",
            "Sounds",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var animation in result.Animations.OrderBy(animation => animation.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(animation.Id),
                Csv(animation.Name),
                Csv(animation.Rotational),
                Csv(string.Join("|", animation.Frames)),
                Csv(string.Join("|", animation.FrameSizes)),
                Csv(string.Join("|", animation.Sounds)),
                Csv(string.Join("|", animation.Flags)),
                Csv(animation.UnknownFields.Count),
                Csv(animation.Issues.Count)));
        }
    }

    private static StreamWriter CreateWriter(string outputPath)
    {
        string? directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        return new StreamWriter(outputPath, false, Encoding.UTF8);
    }

    private static string Csv(object? value)
    {
        if (value is null)
            return "";

        string text = value.ToString() ?? "";

        bool mustQuote =
            text.Contains(',') ||
            text.Contains('"') ||
            text.Contains('\r') ||
            text.Contains('\n');

        if (!mustQuote)
            return text;

        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
}