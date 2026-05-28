using System.Globalization;
using RPGWO.Data.Common;

namespace RPGWO.Data.Definitions;

/// <summary>
/// Represents world.ini, which is a global key/value + bare-flag settings file.
/// Unlike item.ini or monster.ini, world.ini does not contain repeated definition blocks.
/// </summary>
public sealed class WorldDefinition : DefinitionBase
{
    public List<WorldSetting> Settings { get; } = new();

    public List<string> Flags { get; } = new();

    public List<string> UnknownFlags { get; } = new();

    public IEnumerable<WorldSetting> GetSettings(string key)
    {
        return Settings.Where(
            setting => setting.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public string? GetValue(string key)
    {
        return GetSettings(key).LastOrDefault()?.Value;
    }

    public IReadOnlyList<string> GetValues(string key)
    {
        return GetSettings(key)
            .Select(setting => setting.Value)
            .ToList();
    }

    public bool HasFlag(string flagName)
    {
        return Flags.Any(flag => flag.Equals(flagName, StringComparison.OrdinalIgnoreCase));
    }

    public int? GetInt(string key)
    {
        string? value = GetValue(key);

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value.Trim(), out int parsed)
            ? parsed
            : null;
    }

    public double? GetDouble(string key)
    {
        string? value = GetValue(key);

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return double.TryParse(
            value.Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double parsed)
                ? parsed
                : null;
    }

    public bool? GetBool(string key)
    {
        string? value = GetValue(key);

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (bool.TryParse(value.Trim(), out bool parsedBool))
            return parsedBool;

        if (int.TryParse(value.Trim(), out int parsedInt))
            return parsedInt != 0;

        if (value.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("no", StringComparison.OrdinalIgnoreCase))
            return false;

        if (value.Equals("on", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("off", StringComparison.OrdinalIgnoreCase))
            return false;

        return null;
    }
}

/// <summary>
/// One active key=value line from world.ini.
/// </summary>
public sealed class WorldSetting
{
    public required string Key { get; init; }

    public required string Value { get; init; }

    public required int LineNumber { get; init; }

    public required string OriginalText { get; init; }

    public required bool IsKnown { get; init; }
}