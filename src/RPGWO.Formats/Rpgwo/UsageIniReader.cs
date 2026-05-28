using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Converts legacy RPGWO itemuse.ini blocks into typed UsageDefinition objects.
/// itemuse.ini uses a bare "Itemuse" line as the start of each block.
/// </summary>
public static class UsageIniReader
{
    public static UsageIniReadResult ReadFile(string path)
    {
        IniParseResult parseResult = IniParser.ParseFile(path);
        return ReadDocument(parseResult.Document);
    }

    public static UsageIniReadResult ReadDocument(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new UsageIniReadResult(document);

        UsageDefinition? currentUsage = null;
        int nextEntryId = 1;

        foreach (IniLine line in document.Lines)
        {
            switch (line)
            {
                case IniFlagLine flagLine:
                    HandleFlagLine(
                        flagLine,
                        result,
                        ref currentUsage,
                        ref nextEntryId);
                    break;

                case IniKeyValueLine keyValueLine:
                    HandleKeyValueLine(
                        keyValueLine,
                        result,
                        currentUsage);
                    break;
            }
        }

        return result;
    }

    private static void HandleFlagLine(
        IniFlagLine line,
        UsageIniReadResult result,
        ref UsageDefinition? currentUsage,
        ref int nextEntryId)
    {
        string flagName = line.Name.Trim();

        if (string.IsNullOrWhiteSpace(flagName))
            return;

        if (IsNewUsageMarker(flagName))
        {
            currentUsage = new UsageDefinition
            {
                Id = nextEntryId++
            };

            result.Usages.Add(currentUsage);
            return;
        }

        if (currentUsage is null)
        {
            result.GlobalFlags.Add(flagName);
            return;
        }

        currentUsage.Flags.Add(flagName);
        ApplyUsageFlag(currentUsage, flagName);
    }

    private static void HandleKeyValueLine(
        IniKeyValueLine line,
        UsageIniReadResult result,
        UsageDefinition? currentUsage)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (IsNewUsageMarker(key))
        {
            // Some files may theoretically use Itemuse=<value>.
            // We treat it as a marker, but do not use the value as an ID.
            return;
        }

        if (currentUsage is null)
        {
            result.GlobalFields.Add(new UnknownField(
                key: key,
                value: value,
                lineNumber: line.LineNumber,
                originalText: line.OriginalText));

            return;
        }

        ApplyUsageField(currentUsage, line, result);
    }

    private static bool IsNewUsageMarker(string keyOrFlag)
    {
        return keyOrFlag.Equals("Itemuse", StringComparison.OrdinalIgnoreCase)
            || keyOrFlag.Equals("ItemUse", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyUsageFlag(
        UsageDefinition usage,
        string flagName)
    {
        switch (flagName.ToUpperInvariant())
        {
            case "OWNLAND":
                usage.OwnLand = true;
                break;

            case "PUBLICUSE":
                usage.PublicUse = true;
                break;

            case "NOTONPLAYER":
                usage.NotOnPlayer = true;
                break;

            case "PRESERVEDATA":
                usage.PreserveData = true;
                break;

            case "HIDDEN":
                usage.Hidden = true;
                break;

            case "SURFACEONLY":
                usage.SurfaceOnly = true;
                break;

            case "UNDERGROUNDONLY":
                usage.UnderGroundOnly = true;
                break;

            case "DIGUNDERGROUND":
                usage.DigUnderGround = true;
                break;

            case "LOWERLAND":
                usage.LowerLand = true;
                break;

            case "RAISELAND":
                usage.RaiseLand = true;
                break;

            case "PLOTUSE":
                usage.PlotUse = true;
                break;

            case "LOCKFOCUS":
                usage.LockFocus = true;
                break;

            case "RESETARMOR":
                usage.ResetArmor = true;
                break;

            case "RESETWEAPON":
                usage.ResetWeapon = true;
                break;

            case "RESETITEMUSE":
                usage.ResetItemUse = true;
                break;

            case "USEALLQTY":
                usage.UseAllQuantity = true;
                break;

            case "USEITEMSKILLREQ":
                usage.UseItemSkillReq = true;
                break;

            case "REVERSETOOL":
                usage.ReverseTool = true;
                break;

            case "SETFOCUSDATA8":
                usage.SetFocusData8 = true;
                break;

            case "SETWRITING":
                usage.SetWriting = true;
                break;

            case "SHOWWRITING":
                usage.ShowWriting = true;
                break;

            case "DISPKEYFOCUS":
                usage.DisplayKeyFocus = true;
                break;

            case "KEYFOCUS":
                usage.KeyFocus = true;
                break;

            case "PICKLOCK":
                usage.Picklock = true;
                break;

            case "DISARMTRAP":
                usage.DisarmTrap = true;
                break;

            case "MAKEPK":
                usage.MakePk = true;
                break;

            case "MAKENONPK":
                usage.MakeNonPk = true;
                break;

            case "SETRESURRECTSPOT":
                usage.SetResurrectSpot = true;
                break;

            case "RENEWINNROOM":
                usage.RenewInnRoom = true;
                break;

            case "DONOTUSESUCCESSTOOL":
                usage.DoNotUseSuccessTool = true;
                break;

            case "USESUCCESSTOOL":
                usage.UseSuccessTool = true;
                break;

            case "TRIGGER":
                usage.Trigger = true;
                break;

            case "WARP":
                usage.WarpFlag = true;
                break;
        }
    }



    private static void ApplyUsageField(
        UsageDefinition usage,
        IniKeyValueLine line,
        UsageIniReadResult result)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (TryApplyNumberedUsageField(usage, line, result))
            return;

        switch (key.ToUpperInvariant())
        {
            case "ITEMTOOL":
                usage.ItemTool = value;
                break;

            case "ITEMFOCUS":
                usage.ItemFocus = value;
                break;

            case "FOCUSSUBTYPE":
                usage.FocusSubType = value;
                break;

            case "ITEMTOOLQTY":
                usage.ItemToolQuantity = ParseNullableInt(usage, line, result);
                break;

            case "SKILL":
                usage.Skill = value;
                break;

            case "SKILLMIN":
                usage.SkillMin = ParseNullableInt(usage, line, result);
                break;

            case "SKILLMAX":
                usage.SkillMax = ParseNullableInt(usage, line, result);
                break;

            case "SKILLXPSUCCESS":
                usage.SkillXpSuccess = ParseNullableInt(usage, line, result);
                break;

            case "SKILLXPFAILURE":
                usage.SkillXpFailure = ParseNullableInt(usage, line, result);
                break;

            case "STAMINACOST":
                usage.StaminaCost = ParseNullableInt(usage, line, result);
                break;

            case "RANGE":
                usage.Range = ParseNullableInt(usage, line, result);
                break;

            case "ANIMATION":
                usage.Animation = ParseNullableInt(usage, line, result);
                break;

            case "SUCCESSTOOL":
                usage.SuccessTool = value;
                break;

            case "SUCCESSFOCUS":
                usage.SuccessFocus = value;
                break;

            case "FAILEDTOOL":
                usage.FailedTool = value;
                break;

            case "FAILEDFOCUS":
                usage.FailedFocus = value;
                break;

            case "FAILEDDAMAGE":
                usage.FailedDamage = ParseNullableInt(usage, line, result);
                break;

            case "SUCCESSITEM":
                usage.SuccessItems.Add(value);
                break;

            case "SUCCESSITEMQTY":
                AddParsedIntToList(
                    usage.SuccessItemQuantities,
                    usage,
                    line,
                    result);
                break;

            case "FAILEDITEM":
                usage.FailedItems.Add(value);
                break;

            case "FAILEDITEMQTY":
                AddParsedIntToList(
                    usage.FailedItemQuantities,
                    usage,
                    line,
                    result);
                break;

            case "NEEDFLATSURFACE":
                usage.NeedFlatSurface = ParseNullableBool(usage, line, result);
                break;

            case "NEEDUNLEVELSURFACE":
                usage.NeedUnLevelSurface = ParseNullableBool(usage, line, result);
                break;

            case "SURFACEGROUND":
                usage.SurfaceGround = value;
                break;

            case "SURFACEUNDERGROUND":
                usage.SurfaceUnderGround = value;
                break;

            case "SURFACEWATER":
                usage.SurfaceWater = value;
                break;

            case "USEPLAYERPOSITION":
                usage.UsePlayerPosition = ParseNullableBool(usage, line, result);
                break;

            case "SUCCESSMSG":
                usage.SuccessMessage = value;
                break;

            case "FAILEDMSG":
                usage.FailedMessage = value;
                break;

            case "MONSTERID":
                usage.MonsterId = ParseNullableInt(usage, line, result);
                break;

            case "PLAYERUSAGETIMEOUT":
                usage.PlayerUsageTimeout = ParseNullableInt(usage, line, result);
                break;

            case "GIVESKILLBONUS":
                usage.GiveSkillBonus = ParseNullableInt(usage, line, result);
                break;

            case "GUILD":
                usage.Guild = value;
                break;

            case "DRUNK":
                usage.Drunk = ParseNullableInt(usage, line, result);
                break;

            case "HEAL":
                usage.Heal = ParseNullableInt(usage, line, result);
                break;

            case "HEALPOISON":
                usage.HealPoison = ParseNullableInt(usage, line, result);
                break;

            case "WARP":
                usage.Warp = value;
                break;

            case "TOOLSUBTYPE":
                usage.ToolSubType = value;
                break;

            case "ITEMFOCUSQTY":
                usage.ItemFocusQuantity = ParseNullableInt(usage, line, result);
                break;

            case "SKILL2":
                usage.Skill2 = value;
                break;

            case "BUILDITEM":
                usage.BuildItem = value;
                break;

            case "BUILDNEEDED":
                usage.BuildNeeded = ParseNullableInt(usage, line, result);
                break;

            case "BUILDWORK":
                usage.BuildWork = ParseNullableInt(usage, line, result);
                break;

            case "MANA":
                usage.Mana = ParseNullableInt(usage, line, result);
                break;

            case "REVIVE":
                usage.Revive = ParseNullableInt(usage, line, result);
                break;

            case "SETFOCUSDATA1":
                usage.SetFocusData1 = ParseNullableInt(usage, line, result);
                break;

            case "KEYFOCUS":
                usage.KeyFocusValue = value;
                break;

            case "PICKLOCK":
                usage.PicklockValue = value;
                break;

            case "REVERSETOOL":
                usage.ReverseToolValue = value;
                break;




            default:
                usage.UnknownFields.Add(new UnknownField(
                    key: key,
                    value: value,
                    lineNumber: line.LineNumber,
                    originalText: line.OriginalText));
                break;
        }
    }

    private static bool TryApplyNumberedUsageField(
    UsageDefinition usage,
    IniKeyValueLine line,
    UsageIniReadResult result)
    {
        string key = line.Key.Trim();

        if (TryMatchNumberedKey(key, "SuccessItemQty", out _))
        {
            AddParsedIntToList(
                usage.SuccessItemQuantities,
                usage,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "SuccessItemQTY", out _))
        {
            AddParsedIntToList(
                usage.SuccessItemQuantities,
                usage,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "SuccessItem", out _))
        {
            usage.SuccessItems.Add(line.Value);
            return true;
        }

        if (TryMatchNumberedKey(key, "FailedItemQty", out _))
        {
            AddParsedIntToList(
                usage.FailedItemQuantities,
                usage,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "FailedItemQTY", out _))
        {
            AddParsedIntToList(
                usage.FailedItemQuantities,
                usage,
                line,
                result);

            return true;
        }

        if (TryMatchNumberedKey(key, "FailedItem", out _))
        {
            usage.FailedItems.Add(line.Value);
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
        UsageDefinition usage,
        IniKeyValueLine line,
        UsageIniReadResult result)
    {
        int? parsed = ParseNullableInt(usage, line, result);

        if (parsed is not null)
            list.Add(parsed.Value);
    }

    private static int? ParseNullableInt(
        UsageDefinition usage,
        IniKeyValueLine line,
        UsageIniReadResult result)
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

        usage.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool? ParseNullableBool(
        UsageDefinition usage,
        IniKeyValueLine line,
        UsageIniReadResult result)
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

        usage.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }
}