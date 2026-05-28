using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Converts legacy RPGWO skill.ini lines into typed SkillDefinition objects.
/// RPGWO skill.ini uses Skill=&lt;id&gt; as the start of each skill definition.
/// Bare lines such as FreeSkill, BurdenFactor, Str, Dex, etc. are skill flags.
/// </summary>
public static class SkillIniReader
{
    public static SkillIniReadResult ReadFile(string path)
    {
        IniParseResult parseResult = IniParser.ParseFile(path);
        return ReadDocument(parseResult.Document);
    }

    public static SkillIniReadResult ReadDocument(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new SkillIniReadResult(document);

        SkillDefinition? currentSkill = null;
        int fallbackId = 0;

        foreach (IniLine line in document.Lines)
        {
            switch (line)
            {
                case IniKeyValueLine keyValueLine:
                    HandleKeyValueLine(
                        keyValueLine,
                        result,
                        ref currentSkill,
                        ref fallbackId);
                    break;

                case IniFlagLine flagLine:
                    HandleFlagLine(
                        flagLine,
                        result,
                        currentSkill);
                    break;
            }
        }

        return result;
    }

    private static void HandleKeyValueLine(
        IniKeyValueLine line,
        SkillIniReadResult result,
        ref SkillDefinition? currentSkill,
        ref int fallbackId)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (IsNewSkillMarker(key))
        {
            currentSkill = new SkillDefinition
            {
                Id = TryParseInt(value, out int parsedId)
                    ? parsedId
                    : ++fallbackId
            };

            result.Skills.Add(currentSkill);

            if (!TryParseInt(value, out _))
            {
                var issue = new DefinitionParseIssue(
                    message: "Skill marker did not contain a numeric ID. A fallback ID was assigned.",
                    lineNumber: line.LineNumber,
                    key: key,
                    value: value);

                currentSkill.Issues.Add(issue);
                result.Issues.Add(issue);
            }

            return;
        }

        if (currentSkill is null)
        {
            result.GlobalFields.Add(new UnknownField(
                key: key,
                value: value,
                lineNumber: line.LineNumber,
                originalText: line.OriginalText));

            return;
        }

        ApplySkillField(currentSkill, line, result);
    }

    private static void HandleFlagLine(
        IniFlagLine line,
        SkillIniReadResult result,
        SkillDefinition? currentSkill)
    {
        string flagName = line.Name.Trim();

        if (string.IsNullOrWhiteSpace(flagName))
            return;

        if (currentSkill is null)
        {
            result.GlobalFlags.Add(flagName);
            return;
        }

        currentSkill.Flags.Add(flagName);

        switch (flagName.ToUpperInvariant())
        {
            case "STR":
                currentSkill.Strength = true;
                break;

            case "DEX":
                currentSkill.Dexterity = true;
                break;

            case "QUICK":
                currentSkill.Quickness = true;
                break;

            case "INT":
            case "INTEL":
                currentSkill.Intelligence = true;
                break;

            case "WIS":
            case "WISDOM":
                currentSkill.Wisdom = true;
                break;

            case "BURDENFACTOR":
                currentSkill.BurdenFactor = true;
                break;

            case "SPECIALFEATURE":
                currentSkill.SpecialFeature = true;
                break;

            case "FREESKILL":
                currentSkill.FreeSkill = true;
                break;

            case "LEVELREQ":
                currentSkill.LevelReq = true;
                break;

            case "EXCLUDESKILL":
                currentSkill.ExcludeSkill = true;
                break;
        }
    }

    private static bool IsNewSkillMarker(string key)
    {
        return key.Equals("Skill", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SkillID", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplySkillField(
        SkillDefinition skill,
        IniKeyValueLine line,
        SkillIniReadResult result)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        switch (key.ToUpperInvariant())
        {
            case "NAME":
                skill.Name = value;
                break;

            case "USABLE":
                skill.Usable = ParseNullableBool(skill, line, result);
                break;

            case "SKILLPOINTS":
                skill.SkillPoints = ParseNullableInt(skill, line, result);
                break;

            case "STR":
                skill.Strength = ParseNullableBool(skill, line, result);
                break;

            case "DEX":
                skill.Dexterity = ParseNullableBool(skill, line, result);
                break;

            case "QUICK":
                skill.Quickness = ParseNullableBool(skill, line, result);
                break;

            case "INT":
            case "INTEL":
                skill.Intelligence = ParseNullableBool(skill, line, result);
                break;

            case "WIS":
            case "WISDOM":
                skill.Wisdom = ParseNullableBool(skill, line, result);
                break;

            case "DIVISOR":
                skill.Divisor = ParseNullableInt(skill, line, result);
                break;

            case "BURDENFACTOR":
                skill.BurdenFactor = ParseNullableBool(skill, line, result);
                break;

            case "DESCRIPTION":
                skill.Description = value;
                break;

            case "PURPOSE":
                skill.Purpose = value;
                break;

            case "SPECIALFEATURE":
                skill.SpecialFeature = ParseNullableBool(skill, line, result);
                break;

            case "FREESKILL":
                skill.FreeSkill = ParseNullableBool(skill, line, result);
                break;

            case "LEVELREQ":
                skill.LevelReq = ParseNullableBool(skill, line, result);
                break;

            case "EXCLUDESKILL":
                skill.ExcludeSkill = ParseNullableBool(skill, line, result);
                break;

            default:
                skill.UnknownFields.Add(new UnknownField(
                    key: key,
                    value: value,
                    lineNumber: line.LineNumber,
                    originalText: line.OriginalText));
                break;
        }
    }

    private static int? ParseNullableInt(
        SkillDefinition skill,
        IniKeyValueLine line,
        SkillIniReadResult result)
    {
        string value = line.Value.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, out int parsed))
            return parsed;

        var issue = new DefinitionParseIssue(
            message: "Expected integer value.",
            lineNumber: line.LineNumber,
            key: line.Key,
            value: line.Value);

        skill.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool? ParseNullableBool(
        SkillDefinition skill,
        IniKeyValueLine line,
        SkillIniReadResult result)
    {
        string value = line.Value.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (bool.TryParse(value, out bool parsedBool))
            return parsedBool;

        if (int.TryParse(value, out int parsedInt))
            return parsedInt != 0;

        if (value.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("no", StringComparison.OrdinalIgnoreCase))
            return false;

        var issue = new DefinitionParseIssue(
            message: "Expected boolean value.",
            lineNumber: line.LineNumber,
            key: line.Key,
            value: line.Value);

        skill.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool TryParseInt(string value, out int parsed)
    {
        return int.TryParse(value.Trim(), out parsed);
    }
}