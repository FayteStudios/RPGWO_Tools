using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Converts legacy RPGWO animation.ini lines into typed AnimationDefinition objects.
/// Supported terms confirmed from server2.exe strings:
/// ANIMATION=, ROTATIONAL, FRAME=, FRAMESIZE=, SOUND=.
/// </summary>
public static class AnimationIniReader
{
    public static AnimationIniReadResult ReadFile(string path)
    {
        IniParseResult parseResult = IniParser.ParseFile(path);
        return ReadDocument(parseResult.Document);
    }

    public static AnimationIniReadResult ReadDocument(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new AnimationIniReadResult(document);

        AnimationDefinition? currentAnimation = null;
        int fallbackId = 0;

        foreach (IniLine line in document.Lines)
        {
            switch (line)
            {
                case IniKeyValueLine keyValueLine:
                    HandleKeyValueLine(
                        keyValueLine,
                        result,
                        ref currentAnimation,
                        ref fallbackId);
                    break;

                case IniFlagLine flagLine:
                    HandleFlagLine(
                        flagLine,
                        result,
                        currentAnimation);
                    break;
            }
        }

        return result;
    }

    private static void HandleKeyValueLine(
        IniKeyValueLine line,
        AnimationIniReadResult result,
        ref AnimationDefinition? currentAnimation,
        ref int fallbackId)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        if (IsNewAnimationMarker(key))
        {
            currentAnimation = new AnimationDefinition
            {
                Id = TryParseInt(value, out int parsedId)
                    ? parsedId
                    : ++fallbackId
            };

            result.Animations.Add(currentAnimation);

            if (!TryParseInt(value, out _))
            {
                var issue = new DefinitionParseIssue(
                    message: "Animation marker did not contain a numeric ID. A fallback ID was assigned.",
                    lineNumber: line.LineNumber,
                    key: key,
                    value: value);

                currentAnimation.Issues.Add(issue);
                result.Issues.Add(issue);
            }

            return;
        }

        if (currentAnimation is null)
        {
            result.GlobalFields.Add(new UnknownField(
                key: key,
                value: value,
                lineNumber: line.LineNumber,
                originalText: line.OriginalText));

            return;
        }

        ApplyAnimationField(currentAnimation, line, result);
    }

    private static void HandleFlagLine(
        IniFlagLine line,
        AnimationIniReadResult result,
        AnimationDefinition? currentAnimation)
    {
        string flagName = line.Name.Trim();

        if (string.IsNullOrWhiteSpace(flagName))
            return;

        if (currentAnimation is null)
        {
            result.GlobalFlags.Add(flagName);
            return;
        }

        currentAnimation.Flags.Add(flagName);

        switch (flagName.ToUpperInvariant())
        {
            case "ROTATIONAL":
                currentAnimation.Rotational = true;
                break;
        }
    }

    private static bool IsNewAnimationMarker(string key)
    {
        return key.Equals("Animation", StringComparison.OrdinalIgnoreCase)
            || key.Equals("AnimationID", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyAnimationField(
        AnimationDefinition animation,
        IniKeyValueLine line,
        AnimationIniReadResult result)
    {
        string key = line.Key.Trim();
        string value = line.Value;

        switch (key.ToUpperInvariant())
        {
            case "NAME":
                animation.Name = value;
                break;

            case "FRAME":
                AddParsedIntToList(
                    animation.Frames,
                    animation,
                    line,
                    result);
                break;

            case "FRAMESIZE":
                AddParsedIntToList(
                    animation.FrameSizes,
                    animation,
                    line,
                    result);
                break;

            case "SOUND":
                animation.Sounds.Add(value);
                break;

            default:
                animation.UnknownFields.Add(new UnknownField(
                    key: key,
                    value: value,
                    lineNumber: line.LineNumber,
                    originalText: line.OriginalText));
                break;
        }
    }

    private static void AddParsedIntToList(
        List<int> list,
        AnimationDefinition animation,
        IniKeyValueLine line,
        AnimationIniReadResult result)
    {
        int? parsed = ParseNullableInt(animation, line, result);

        if (parsed is not null)
            list.Add(parsed.Value);
    }

    private static int? ParseNullableInt(
        AnimationDefinition animation,
        IniKeyValueLine line,
        AnimationIniReadResult result)
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

        animation.Issues.Add(issue);
        result.Issues.Add(issue);

        return null;
    }

    private static bool TryParseInt(string value, out int parsed)
    {
        return int.TryParse(value.Trim(), out parsed);
    }
}