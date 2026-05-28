using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;
using System.Globalization;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Converts legacy RPGWO magic.ini lines into typed MagicDefinition objects.
/// magic.ini uses Spell=&lt;id&gt; as the start of each spell block.
/// </summary>
public static class MagicIniReader
{
    public static MagicIniReadResult ReadFile(string path)
    {
        IniParseResult parseResult = IniParser.ParseFile(path);
        return ReadDocument(parseResult.Document);
    }

    public static MagicIniReadResult ReadDocument(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new MagicIniReadResult(document);

        MagicDefinition? currentSpell = null;
        int fallbackId = 0;

        foreach (IniLine line in document.Lines)
        {
            switch (line)
            {
                case IniKeyValueLine keyValueLine:
                    HandleKeyValueLine(
                        keyValueLine,
                        result,
                        ref currentSpell,
                        ref fallbackId);
                    break;

                case IniFlagLine flagLine:
                    HandleFlagLine(
                        flagLine,
                        result,
                        currentSpell);
                    break;
            }
        }

        return result;
    }

    private static void HandleKeyValueLine(
        IniKeyValueLine line,
        MagicIniReadResult result,
        ref MagicDefinition? currentSpell,
        ref int fallbackId)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (IsNewSpellMarker(key))
        {
            currentSpell = new MagicDefinition
            {
                Id = TryParseInt(value, out int parsedId)
                    ? parsedId
                    : ++fallbackId
            };

            result.Spells.Add(currentSpell);

            if (!TryParseInt(value, out _))
            {
                var issue = new DefinitionParseIssue(
                    message: "Spell marker did not contain a numeric ID. A fallback ID was assigned.",
                    lineNumber: line.LineNumber,
                    key: key,
                    value: value);

                currentSpell.Issues.Add(issue);
                result.Issues.Add(issue);
            }

            return;
        }

        if (currentSpell is null)
        {
            result.GlobalFields.Add(new UnknownField(
                key: key,
                value: value,
                lineNumber: line.LineNumber,
                originalText: line.OriginalText));

            return;
        }

        ApplyMagicField(currentSpell, line, result);
    }

    private static void HandleFlagLine(
        IniFlagLine line,
        MagicIniReadResult result,
        MagicDefinition? currentSpell)
    {
        string flagName = line.Name.Trim();

        if (string.IsNullOrWhiteSpace(flagName))
            return;

        if (currentSpell is null)
        {
            result.GlobalFlags.Add(flagName);
            return;
        }

        currentSpell.Flags.Add(flagName);
        ApplyMagicFlag(currentSpell, flagName);
    }

    private static bool IsNewSpellMarker(string key)
    {
        return key.Equals("Spell", StringComparison.OrdinalIgnoreCase)
            || key.Equals("SpellID", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyMagicFlag(
        MagicDefinition spell,
        string flagName)
    {
        switch (flagName.ToUpperInvariant())
        {
            case "PERK":
                spell.Perk = true;
                break;

            case "DEITYONLY":
                spell.DeityOnly = true;
                break;

            case "IGNOREWANDPOWER":
                spell.IgnoreWandPower = true;
                break;

            case "LINEOFSIGHT":
                spell.LineOfSight = true;
                break;

            case "ALLOWDEFEND":
                spell.AllowDefend = true;
                break;

            case "SPECIALFEATURE":
                spell.SpecialFeature = true;
                break;

            case "NOTONOTHERSLAND":
                spell.NotOnOthersLand = true;
                break;

            case "LOGHISTORY":
                spell.LogHistory = true;
                break;

            case "ALLOWONPLOT":
                spell.AllowOnPlot = true;
                break;

            case "WARPMEMORIZE":
                spell.WarpMemorize = true;
                break;

            case "WARP":
                spell.Warp = true;
                break;

            case "CREATEWARPSTONE":
                spell.CreateWarpStone = true;
                break;

            case "LOCK":
                spell.Lock = true;
                break;

            case "ACTIVEPLAYERSONLY":
                spell.ActivePlayersOnly = true;
                break;

            case "MANABOND":
                spell.ManaBond = true;
                break;

            case "REVEALORE":
                spell.RevealOre = true;
                break;
        }
    }

    private static void ApplyMagicField(
        MagicDefinition spell,
        IniKeyValueLine line,
        MagicIniReadResult result)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (TryApplyNumberedMagicField(spell, line, result))
            return;

        switch (key.ToUpperInvariant())
        {
            case "NAME":
                spell.Name = value;
                break;

            case "DESCRIPTION":
                spell.Description = value;
                break;

            case "SKILL":
                spell.Skill = value;
                break;

            case "SKILLTOLEARN":
                spell.SkillToLearn = value;
                break;

            case "SKILLMIN":
                spell.SkillMin = ParseNullableInt(spell, line, result);
                break;

            case "SKILLMAX":
                spell.SkillMax = ParseNullableInt(spell, line, result);
                break;

            case "WANDUSE":
                spell.WandUse = ParseNullableInt(spell, line, result);
                break;

            case "MANACOST":
                spell.ManaCost = ParseNullableInt(spell, line, result);
                break;

            case "RANGE":
                spell.Range = ParseNullableInt(spell, line, result);
                break;

            case "TARGET":
                spell.Target = value;
                break;

            case "CASTTIME":
                spell.CastTime = ParseNullableDouble(spell, line, result);
                break;

            case "SUCCESSXP":
                spell.SuccessXp = ParseNullableInt(spell, line, result);
                break;

            case "FAILEDXP":
                spell.FailedXp = ParseNullableInt(spell, line, result);
                break;

            case "PROJECTILEANIMATION":
                spell.ProjectileAnimation = value;
                break;

            case "SOUND":
                spell.Sound = value;
                break;

            case "VARIANCE":
                spell.Variance = ParseNullableInt(spell, line, result);
                break;

            case "LIFE":
                spell.Life = ParseNullableInt(spell, line, result);
                break;

            case "LIFERENEWAL":
                spell.LifeRenewal = ParseNullableInt(spell, line, result);
                break;

            case "LIFESTEAL":
                spell.LifeSteal = ParseNullableInt(spell, line, result);
                break;

            case "STAMINA":
                spell.Stamina = ParseNullableInt(spell, line, result);
                break;

            case "STAMINARENEWAL":
                spell.StaminaRenewal = ParseNullableInt(spell, line, result);
                break;

            case "STAMINASTEAL":
                spell.StaminaSteal = ParseNullableInt(spell, line, result);
                break;

            case "MANA":
                spell.Mana = ParseNullableInt(spell, line, result);
                break;

            case "MANARENEWAL":
                spell.ManaRenewal = ParseNullableInt(spell, line, result);
                break;

            case "MANASTEAL":
                spell.ManaSteal = ParseNullableInt(spell, line, result);
                break;

            case "CURE":
                spell.Cure = ParseNullableInt(spell, line, result);
                break;

            case "ICE":
                spell.Ice = ParseNullableInt(spell, line, result);
                break;

            case "BLIND":
                spell.Blind = ParseNullableInt(spell, line, result);
                break;

            case "HERO":
                spell.Hero = ParseNullableInt(spell, line, result);
                break;

            case "STRENGTH":
                spell.Strength = ParseNullableInt(spell, line, result);
                break;

            case "DEXTERITY":
                spell.Dexterity = ParseNullableInt(spell, line, result);
                break;

            case "QUICKNESS":
                spell.Quickness = ParseNullableInt(spell, line, result);
                break;

            case "INTELLIGENCE":
            case "INTEL":
                spell.Intelligence = ParseNullableInt(spell, line, result);
                break;

            case "WISDOM":
                spell.Wisdom = ParseNullableInt(spell, line, result);
                break;

            case "ARMOR":
                spell.Armor = ParseNullableInt(spell, line, result);
                break;

            case "IMPROVE":
                spell.Improve = ParseNullableInt(spell, line, result);
                break;

            case "ESSENCESTEAL":
                spell.EssenceSteal = ParseNullableInt(spell, line, result);
                break;

            case "DAMAGETYPE":
                spell.DamageType = value;
                break;

            case "SPAWNITEM":
                spell.SpawnItems.Add(value);
                break;

            case "SPAWNITEMQTY":
            case "SPAWNITEMQUANTITY":
                AddParsedIntToList(
                    spell.SpawnItemQuantities,
                    spell,
                    line,
                    result);
                break;

            case "TRANSFORMFROM":
                spell.TransformFrom = value;
                break;

            case "TRANSFORMTO":
                spell.TransformTo = value;
                break;

            case "GOLEMITEM":
                spell.GolemItem = value;
                break;

            case "GOLEMMONSTER":
                spell.GolemMonster = value;
                break;

            case "GOLEMSKILL":
                spell.GolemSkill = value;
                break;

            default:
                spell.UnknownFields.Add(new UnknownField(
                    key: key,
                    value: value,
                    lineNumber: line.LineNumber,
                    originalText: line.OriginalText));
                break;
        }
    }

    private static double? ParseNullableDouble(
    MagicDefinition spell,
    IniKeyValueLine line,
    MagicIniReadResult result)
    {
        string value = line.Value.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsed))
        {
            return parsed;
        }

        var issue = new DefinitionParseIssue(
            message: "Expected decimal number value.",
            lineNumber: line.LineNumber,
            key: line.Key,
            value: line.Value);

        spell.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }
    private static bool TryApplyNumberedMagicField(
        MagicDefinition spell,
        IniKeyValueLine line,
        MagicIniReadResult result)
    {
        string key = line.Key.Trim();

        if (TryMatchNumberedKey(key, "RuneUse", out _))
        {
            AddParsedIntToList(
                spell.RuneUses,
                spell,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "Rune", out _))
        {
            spell.Runes.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "Animation", out _))
        {
            int? parsed = ParseNullableInt(spell, line, result);

            if (parsed is not null)
                spell.Animations.Add(parsed.Value);

            return true;
        }

        return false;
    }

    private static bool TryMatchNumberedKey(
        string key,
        string prefix,
        out int index)
    {
        index = -1;

        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string suffix = key[prefix.Length..];

        return int.TryParse(suffix, out index);
    }

    private static void AddParsedIntToList(
        List<int> list,
        MagicDefinition spell,
        IniKeyValueLine line,
        MagicIniReadResult result)
    {
        int? parsed = ParseNullableInt(spell, line, result);

        if (parsed is not null)
            list.Add(parsed.Value);
    }

    private static int? ParseNullableInt(
        MagicDefinition spell,
        IniKeyValueLine line,
        MagicIniReadResult result)
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

        spell.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool TryParseInt(string value, out int parsed)
    {
        return int.TryParse(value.Trim(), out parsed);
    }
}