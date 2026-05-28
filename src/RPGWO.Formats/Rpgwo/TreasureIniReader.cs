using System.Globalization;
using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Converts legacy RPGWO treasure.ini lines into typed TreasureDefinition objects.
/// treasure.ini is expected to use Treasure=&lt;id&gt; as the start of each treasure block.
/// </summary>
public static class TreasureIniReader
{
    public static TreasureIniReadResult ReadFile(string path)
    {
        IniParseResult parseResult = IniParser.ParseFile(path);
        return ReadDocument(parseResult.Document);
    }

    public static TreasureIniReadResult ReadDocument(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new TreasureIniReadResult(document);

        TreasureDefinition? currentTreasure = null;
        int fallbackId = 0;

        foreach (IniLine line in document.Lines)
        {
            switch (line)
            {
                case IniKeyValueLine keyValueLine:
                    HandleKeyValueLine(
                        keyValueLine,
                        result,
                        ref currentTreasure,
                        ref fallbackId);
                    break;

                case IniFlagLine flagLine:
                    HandleFlagLine(
                        flagLine,
                        result,
                        currentTreasure);
                    break;
            }
        }

        return result;
    }

    private static void HandleKeyValueLine(
        IniKeyValueLine line,
        TreasureIniReadResult result,
        ref TreasureDefinition? currentTreasure,
        ref int fallbackId)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (IsNewTreasureMarker(key))
        {
            bool hasNumericId = TryParseInt(value, out int parsedId);

            currentTreasure = new TreasureDefinition
            {
                Id = hasNumericId
                    ? parsedId
                    : ++fallbackId,

                TreasureName = value,
                Name = value
            };

            result.Treasures.Add(currentTreasure);
            return;
        }

        if (currentTreasure is null)
        {
            result.GlobalFields.Add(new UnknownField(
                key: key,
                value: value,
                lineNumber: line.LineNumber,
                originalText: line.OriginalText));

            return;
        }

        ApplyTreasureField(currentTreasure, line, result);
    }

    private static void HandleFlagLine(
        IniFlagLine line,
        TreasureIniReadResult result,
        TreasureDefinition? currentTreasure)
    {
        string flagName = line.Name.Trim();

        if (string.IsNullOrWhiteSpace(flagName))
            return;

        if (currentTreasure is null)
        {
            result.GlobalFlags.Add(flagName);
            return;
        }

        currentTreasure.Flags.Add(flagName);
        ApplyTreasureFlag(currentTreasure, flagName);
    }

    private static bool IsNewTreasureMarker(string key)
    {
        return key.Equals("Treasure", StringComparison.OrdinalIgnoreCase)
            || key.Equals("TreasureID", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyTreasureFlag(
        TreasureDefinition treasure,
        string flagName)
    {
        switch (flagName.ToUpperInvariant())
        {
            case "NODROP":
                treasure.NoDrop = true;
                break;

            case "RANDOM":
                treasure.Random = true;
                break;

            case "ALWAYS":
                treasure.Always = true;
                break;

            case "ONEONLY":
                treasure.OneOnly = true;
                break;

            case "NOECONOMYVALUEDROP":
                treasure.NoEconomyValueDrop = true;
                break;


        }
    }

    private static void ApplyTreasureField(
        TreasureDefinition treasure,
        IniKeyValueLine line,
        TreasureIniReadResult result)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (TryApplyNumberedTreasureField(treasure, line, result))
            return;

        switch (key.ToUpperInvariant())
        {
            case "NAME":
                treasure.Name = value;
                break;

            case "TREASURECOUNT":
            case "COUNT":
                treasure.TreasureCount = ParseNullableInt(treasure, line, result);
                break;

            case "GOLD":
                treasure.Gold = ParseNullableInt(treasure, line, result);
                break;

            case "GOLDMIN":
                treasure.GoldMin = ParseNullableInt(treasure, line, result);
                break;

            case "GOLDMAX":
                treasure.GoldMax = ParseNullableInt(treasure, line, result);
                break;

            case "MONEY":
                treasure.Money = ParseNullableInt(treasure, line, result);
                break;

            case "MONEYMIN":
                treasure.MoneyMin = ParseNullableInt(treasure, line, result);
                break;

            case "MONEYMAX":
                treasure.MoneyMax = ParseNullableInt(treasure, line, result);
                break;

            case "CHANCE":
                treasure.Chance = ParseNullableDouble(treasure, line, result);
                break;

            case "QTY":
            case "QUANTITY":
                treasure.Quantity = ParseNullableInt(treasure, line, result);
                break;

            case "QTYMIN":
            case "QUANTITYMIN":
                treasure.QuantityMin = ParseNullableInt(treasure, line, result);
                break;

            case "QTYMAX":
            case "QUANTITYMAX":
                treasure.QuantityMax = ParseNullableInt(treasure, line, result);
                break;

            default:
                treasure.UnknownFields.Add(new UnknownField(
                    key: key,
                    value: value,
                    lineNumber: line.LineNumber,
                    originalText: line.OriginalText));
                break;

            case "ITEM":
                treasure.Items.Add(value);
                break;

            case "SKILLID":
                treasure.SkillIds.Add(value);
                break;

            case "SKILLLOW":
                AddParsedIntToList(
                    treasure.SkillLows,
                    treasure,
                    line,
                    result);
                break;

            case "SKILLHIGH":
                AddParsedIntToList(
                    treasure.SkillHighs,
                    treasure,
                    line,
                    result);
                break;

            case "SPELLID":
                treasure.SpellIds.Add(value);
                break;

            case "SPELLDATA":
                treasure.SpellData.Add(value);
                break;

            case "COST":
                treasure.Cost = ParseNullableInt(treasure, line, result);
                break;
        }
    }

    private static bool TryApplyNumberedTreasureField(
        TreasureDefinition treasure,
        IniKeyValueLine line,
        TreasureIniReadResult result)
    {
        string key = line.Key.Trim();

        if (TryMatchNumberedKey(key, "ItemQtyMin", out _))
        {
            AddParsedIntToList(
                treasure.ItemQuantityMins,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "ItemQtyMax", out _))
        {
            AddParsedIntToList(
                treasure.ItemQuantityMaxes,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "ItemQty", out _) ||
            TryMatchNumberedKey(key, "ItemQTY", out _))
        {
            AddParsedIntToList(
                treasure.ItemQuantities,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "ItemChance", out _))
        {
            AddParsedDoubleToList(
                treasure.ItemChances,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "ItemData1", out _))
        {
            treasure.ItemData1.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "ItemData2", out _))
        {
            treasure.ItemData2.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "ItemData3", out _))
        {
            treasure.ItemData3.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "ItemData4", out _))
        {
            treasure.ItemData4.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "ItemText", out _))
        {
            treasure.ItemTexts.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "ItemTotalUses", out _))
        {
            AddParsedIntToList(
                treasure.ItemTotalUses,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "Item", out _))
        {
            treasure.Items.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "GroupChance", out _))
        {
            AddParsedDoubleToList(
                treasure.GroupChances,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "GroupQty", out _))
        {
            AddParsedIntToList(
                treasure.GroupQuantities,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "Group", out _))
        {
            treasure.Groups.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "CatagoryChance", out _) ||
            TryMatchNumberedKey(key, "CategoryChance", out _))
        {
            AddParsedDoubleToList(
                treasure.CatagoryChances,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "CatagoryQty", out _) ||
            TryMatchNumberedKey(key, "CategoryQty", out _))
        {
            AddParsedIntToList(
                treasure.CatagoryQuantities,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "Catagory", out _) ||
            TryMatchNumberedKey(key, "Category", out _))
        {
            treasure.Catagories.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "TreasureChance", out _))
        {
            AddParsedDoubleToList(
                treasure.TreasureRefChances,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "TreasureQty", out _))
        {
            AddParsedIntToList(
                treasure.TreasureRefQuantities,
                treasure,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "Treasure", out _))
        {
            int? parsed = ParseNullableInt(treasure, line, result);

            if (parsed is not null)
                treasure.TreasureRefs.Add(parsed.Value);

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
        TreasureDefinition treasure,
        IniKeyValueLine line,
        TreasureIniReadResult result)
    {
        int? parsed = ParseNullableInt(treasure, line, result);

        if (parsed is not null)
            list.Add(parsed.Value);
    }

    private static void AddParsedDoubleToList(
        List<double> list,
        TreasureDefinition treasure,
        IniKeyValueLine line,
        TreasureIniReadResult result)
    {
        double? parsed = ParseNullableDouble(treasure, line, result);

        if (parsed is not null)
            list.Add(parsed.Value);
    }

    private static int? ParseNullableInt(
        TreasureDefinition treasure,
        IniKeyValueLine line,
        TreasureIniReadResult result)
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

        treasure.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static double? ParseNullableDouble(
        TreasureDefinition treasure,
        IniKeyValueLine line,
        TreasureIniReadResult result)
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

        treasure.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool TryParseInt(string value, out int parsed)
    {
        return int.TryParse(value.Trim(), out parsed);
    }
}