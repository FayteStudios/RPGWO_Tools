using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Converts legacy RPGWO multiuse.ini blocks into typed MultiUseDefinition objects.
/// multiuse.ini uses a bare "MultiUse" line as the start of each recipe block.
/// </summary>
public static class MultiUseIniReader
{
    public static MultiUseIniReadResult ReadFile(string path)
    {
        IniParseResult parseResult = IniParser.ParseFile(path);
        return ReadDocument(parseResult.Document);
    }

    public static MultiUseIniReadResult ReadDocument(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new MultiUseIniReadResult(document);

        MultiUseDefinition? currentRecipe = null;
        int nextRecipeId = 1;

        foreach (IniLine line in document.Lines)
        {
            switch (line)
            {
                case IniFlagLine flagLine:
                    HandleFlagLine(
                        flagLine,
                        result,
                        ref currentRecipe,
                        ref nextRecipeId);
                    break;

                case IniKeyValueLine keyValueLine:
                    HandleKeyValueLine(
                        keyValueLine,
                        result,
                        currentRecipe);
                    break;
            }
        }

        return result;
    }

    private static void HandleFlagLine(
        IniFlagLine line,
        MultiUseIniReadResult result,
        ref MultiUseDefinition? currentRecipe,
        ref int nextRecipeId)
    {
        string flagName = line.Name.Trim();

        if (string.IsNullOrWhiteSpace(flagName))
            return;

        if (IsNewRecipeMarker(flagName))
        {
            currentRecipe = new MultiUseDefinition
            {
                Id = nextRecipeId++
            };

            result.Recipes.Add(currentRecipe);
            return;
        }

        if (currentRecipe is null)
        {
            result.GlobalFlags.Add(flagName);
            return;
        }

        currentRecipe.Flags.Add(flagName);
    }

    private static void HandleKeyValueLine(
        IniKeyValueLine line,
        MultiUseIniReadResult result,
        MultiUseDefinition? currentRecipe)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (IsNewRecipeMarker(key))
        {
            // Some files may theoretically use MultiUse=<value>.
            // We treat it as a marker, but do not use the value as an ID.
            return;
        }

        if (currentRecipe is null)
        {
            result.GlobalFields.Add(new UnknownField(
                key: key,
                value: value,
                lineNumber: line.LineNumber,
                originalText: line.OriginalText));

            return;
        }

        ApplyRecipeField(currentRecipe, line, result);
    }

    private static bool IsNewRecipeMarker(string keyOrFlag)
    {
        return keyOrFlag.Equals("MultiUse", StringComparison.OrdinalIgnoreCase)
            || keyOrFlag.Equals("Multiuse", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyRecipeField(
        MultiUseDefinition recipe,
        IniKeyValueLine line,
        MultiUseIniReadResult result)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        switch (key.ToUpperInvariant())
        {
            case "SUCCESSITEM":
                recipe.SuccessItem = value;
                break;

            case "SUCCESSITEMQTY":
            case "SUCCESSITEMQUANTITY":
                recipe.SuccessItemQuantity = ParseNullableInt(recipe, line, result);
                break;

            case "FOCUSITEM":
                recipe.FocusItem = value;
                break;

            case "NEEDITEM":
                recipe.NeedItems.Add(value);
                break;

            case "NEEDITEMQTY":
            case "NEEDITEMQUANTITY":
                AddParsedIntToList(
                    recipe.NeedItemQuantities,
                    recipe,
                    line,
                    result);
                break;

            case "RESULTITEM":
                recipe.ResultItems.Add(value);
                break;

            case "RESULTITEMQTY":
            case "RESULTITEMQUANTITY":
                AddParsedIntToList(
                    recipe.ResultItemQuantities,
                    recipe,
                    line,
                    result);
                break;

            case "SKILL":
                recipe.Skill = value;
                break;

            case "SKILLMIN":
                recipe.SkillMin = ParseNullableInt(recipe, line, result);
                break;

            case "SKILLMAX":
                recipe.SkillMax = ParseNullableInt(recipe, line, result);
                break;

            case "SKILLXPSUCCESS":
                recipe.SkillXpSuccess = ParseNullableInt(recipe, line, result);
                break;

            case "STAMINACOST":
                recipe.StaminaCost = ParseNullableInt(recipe, line, result);
                break;

            case "SUCCESSMSG":
                recipe.SuccessMessage = value;
                break;

            default:
                recipe.UnknownFields.Add(new UnknownField(
                    key: key,
                    value: value,
                    lineNumber: line.LineNumber,
                    originalText: line.OriginalText));
                break;
        }
    }

    private static void AddParsedIntToList(
        List<int> list,
        MultiUseDefinition recipe,
        IniKeyValueLine line,
        MultiUseIniReadResult result)
    {
        int? parsed = ParseNullableInt(recipe, line, result);

        if (parsed is not null)
            list.Add(parsed.Value);
    }

    private static int? ParseNullableInt(
        MultiUseDefinition recipe,
        IniKeyValueLine line,
        MultiUseIniReadResult result)
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

        recipe.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }
}