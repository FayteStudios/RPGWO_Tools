using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RPGWO.ServerTool.Services;

/// <summary>
/// Clean skill.ini editor service modeled after the Python tool behavior.
/// Known skill fields are always exposed to the UI, blank value fields are skipped,
/// checked flags write bare keywords, and Usable writes as Usable=1.
/// </summary>
public sealed class SkillEditorService
{
    private sealed record FieldDef(
        string Label,
        string IniKey,
        string Kind,
        string Group,
        bool Required = false,
        string DefaultValue = "",
        IReadOnlyList<string>? Options = null);

    private sealed class SkillRecord
    {
        public Dictionary<string, object?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int Id => ToInt(Values.GetValueOrDefault("Skill"));
    }

    private static readonly string[] PurposeOptions = { "", "Melee", "Missle", "Magic" };

    private static FieldDef Text(string label, string group = "General", string? iniKey = null, string defaultValue = "", bool required = false)
        => new(label, iniKey ?? label, "text", group, required, defaultValue);

    private static FieldDef LongText(string label, string group = "General", string? iniKey = null, string defaultValue = "")
        => new(label, iniKey ?? label, "longtext", group, false, defaultValue);

    private static FieldDef Int(string label, string group = "General", string? iniKey = null, string defaultValue = "", bool required = false)
        => new(label, iniKey ?? label, "int", group, required, defaultValue);

    private static FieldDef Combo(string label, string group, IReadOnlyList<string> options, string? iniKey = null, string defaultValue = "")
        => new(label, iniKey ?? label, "combo", group, false, defaultValue, options);

    private static FieldDef Flag(string label, string group = "Flags", string? iniKey = null)
        => new(label, iniKey ?? label, "flag", group);

    private static FieldDef ValueBool(string label, string group = "General", string? iniKey = null)
        => new(label, iniKey ?? label, "value_bool", group);

    private static readonly IReadOnlyList<FieldDef> Fields = new List<FieldDef>
    {
        Int("Skill", "Base", required: true),
        Text("Name", "Base", required: true),

        ValueBool("Usable", "General"),
        Int("SkillPoints", "General"),
        LongText("Description", "General"),
        Combo("Purpose", "General", PurposeOptions),

        Flag("Str", "Attributes"),
        Flag("Dex", "Attributes"),
        Flag("Quick", "Attributes"),
        Flag("Int", "Attributes", iniKey: "Intel"),
        Flag("Wis", "Attributes", iniKey: "Wisdom"),
        Int("Divisor", "Attributes"),
        Flag("BurdenFactor", "Attributes"),

        Flag("SpecialFeature", "Flags"),
        Flag("FreeSkill", "Flags"),
        Flag("LevelReq", "Flags"),
        Flag("ExcludeSkill", "Flags"),
    };

    public IReadOnlyList<string> Groups { get; } = Fields.Select(field => field.Group).Distinct().ToList();

    public List<SkillRow> LoadSkillRows(string skillIniPath)
    {
        return ParseSkills(skillIniPath)
            .OrderBy(skill => skill.Id)
            .Select(ToRow)
            .ToList();
    }

    public List<SkillFieldRow> CreateBlankFieldRows(int nextId)
    {
        Dictionary<string, object?> values = BlankValues();
        values["Skill"] = nextId.ToString();
        values["Name"] = "";
        return ToFieldRows(values);
    }

    public List<SkillFieldRow> LoadFieldRows(string skillIniPath, int skillId)
    {
        SkillRecord? record = ParseSkills(skillIniPath).FirstOrDefault(skill => skill.Id == skillId);
        if (record is null)
            throw new InvalidOperationException($"Skill={skillId} was not found.");

        return ToFieldRows(record.Values);
    }

    public string GetSkillPreview(string skillIniPath, int skillId)
    {
        SkillRecord? record = ParseSkills(skillIniPath).FirstOrDefault(skill => skill.Id == skillId);
        return record is null ? "" : string.Join(Environment.NewLine, ToIniLines(record));
    }

    public void SaveSkillRows(string skillIniPath, IReadOnlyList<SkillFieldRow> fieldRows)
    {
        SkillRecord record = FromFieldRows(fieldRows);

        if (record.Id <= 0)
            throw new InvalidOperationException("Skill is required and must be a whole number greater than zero.");

        if (string.IsNullOrWhiteSpace(Clean(record.Values.GetValueOrDefault("Name"))))
            throw new InvalidOperationException("Name is required.");

        List<SkillRecord> skills = File.Exists(skillIniPath) ? ParseSkills(skillIniPath) : new List<SkillRecord>();
        skills.RemoveAll(skill => skill.Id == record.Id);
        skills.Add(record);
        WriteSkills(skillIniPath, skills.OrderBy(skill => skill.Id));
    }

    public void DeleteSkill(string skillIniPath, int skillId)
    {
        List<SkillRecord> skills = ParseSkills(skillIniPath);
        skills.RemoveAll(skill => skill.Id == skillId);
        WriteSkills(skillIniPath, skills.OrderBy(skill => skill.Id));
    }

    private static SkillRow ToRow(SkillRecord skill) => new()
    {
        Id = skill.Id,
        Name = Clean(skill.Values.GetValueOrDefault("Name")),
        Purpose = Clean(skill.Values.GetValueOrDefault("Purpose")),
        Usable = IsTruthy(skill.Values.GetValueOrDefault("Usable")) ? "TRUE" : "",
        SkillPoints = Clean(skill.Values.GetValueOrDefault("SkillPoints")),
        Divisor = Clean(skill.Values.GetValueOrDefault("Divisor"))
    };

    private static Dictionary<string, object?> BlankValues()
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (FieldDef field in Fields)
            values[field.Label] = field.Kind is "flag" or "value_bool" ? false : field.DefaultValue;

        return values;
    }

    private static List<SkillFieldRow> ToFieldRows(Dictionary<string, object?> values)
    {
        return Fields.Select(field => new SkillFieldRow
        {
            Group = field.Group,
            Label = field.Label,
            IniKey = field.IniKey,
            Kind = field.Kind,
            Required = field.Required,
            IsCore = field.Group == "Base",
            Options = field.Options ?? Array.Empty<string>(),
            Enabled = field.Kind is "flag" or "value_bool" && IsTruthy(values.GetValueOrDefault(field.Label)),
            Value = field.Kind is "flag" or "value_bool" ? "" : Clean(values.GetValueOrDefault(field.Label))
        }).ToList();
    }

    private static SkillRecord FromFieldRows(IEnumerable<SkillFieldRow> rows)
    {
        var record = new SkillRecord();

        foreach (FieldDef field in Fields)
        {
            SkillFieldRow? row = rows.FirstOrDefault(candidate => string.Equals(candidate.Label, field.Label, StringComparison.OrdinalIgnoreCase));
            if (row is null)
                continue;

            record.Values[field.Label] = field.Kind is "flag" or "value_bool"
                ? row.Enabled
                : Clean(row.Value);
        }

        return record;
    }

    private static List<SkillRecord> ParseSkills(string path)
    {
        var skills = new List<SkillRecord>();
        var current = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        void Flush()
        {
            if (!current.TryGetValue("Skill", out object? idValue) || string.IsNullOrWhiteSpace(Clean(idValue)))
            {
                current.Clear();
                return;
            }

            Dictionary<string, object?> values = BlankValues();

            foreach (KeyValuePair<string, object?> pair in current)
            {
                FieldDef? field = FieldForKey(pair.Key);
                if (field is null)
                    continue;

                values[field.Label] = field.Kind is "flag" or "value_bool"
                    ? pair.Value is bool boolValue ? boolValue : IsTruthy(pair.Value)
                    : Clean(pair.Value);
            }

            var record = new SkillRecord();

            foreach (KeyValuePair<string, object?> pair in values)
                record.Values[pair.Key] = pair.Value;

            if (record.Id > 0)
                skills.Add(record);

            current.Clear();
        }

        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            if (line.Contains('='))
            {
                string[] parts = line.Split('=', 2);
                string key = parts[0].Trim();
                string value = parts[1].Trim();

                if (key.Equals("Skill", StringComparison.OrdinalIgnoreCase))
                {
                    Flush();
                    current["Skill"] = value;
                }
                else
                {
                    StoreValue(current, key, value);
                }
            }
            else
            {
                StoreValue(current, line, true);
            }
        }

        Flush();
        return skills;
    }

    private static void StoreValue(Dictionary<string, object?> current, string key, object? value)
    {
        FieldDef? field = FieldForKey(key);
        string storageKey = field?.Label ?? key.Trim();
        current[storageKey] = value;
    }

    private static void WriteSkills(string skillIniPath, IEnumerable<SkillRecord> skills)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(skillIniPath)!);

        string output = string.Join(
            Environment.NewLine + Environment.NewLine,
            skills.Select(skill => string.Join(Environment.NewLine, ToIniLines(skill))));

        if (!string.IsNullOrWhiteSpace(output))
            output += Environment.NewLine;

        File.WriteAllText(skillIniPath, output);
    }

    private static IEnumerable<string> ToIniLines(SkillRecord skill)
    {
        yield return $"Skill={skill.Id}";

        string[] outputOrder =
        {
            "Name", "FreeSkill", "Usable", "SkillPoints", "Str", "Dex", "Quick", "Int", "Wis",
            "Divisor", "BurdenFactor", "SpecialFeature", "LevelReq", "ExcludeSkill", "Description", "Purpose"
        };

        var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Skill" };

        foreach (string label in outputOrder)
        {
            FieldDef? field = Fields.FirstOrDefault(candidate => candidate.Label.Equals(label, StringComparison.OrdinalIgnoreCase));
            if (field is not null)
            {
                foreach (string line in FieldToLines(skill, field))
                    yield return line;

                written.Add(field.Label);
            }
        }

        foreach (FieldDef field in Fields)
        {
            if (written.Contains(field.Label))
                continue;

            foreach (string line in FieldToLines(skill, field))
                yield return line;
        }
    }

    private static IEnumerable<string> FieldToLines(SkillRecord skill, FieldDef field)
    {
        object? raw = skill.Values.GetValueOrDefault(field.Label);

        if (field.Kind == "flag")
        {
            if (IsTruthy(raw))
                yield return field.IniKey;

            yield break;
        }

        if (field.Kind == "value_bool")
        {
            if (IsTruthy(raw))
                yield return $"{field.IniKey}=1";

            yield break;
        }

        string text = Clean(raw);
        if (text.Length == 0)
            yield break;

        yield return $"{field.IniKey}={text}";
    }

    private static FieldDef? FieldForKey(string key)
    {
        string normalized = key.Trim().ToLowerInvariant();

        Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["intel"] = "Int",
            ["wisdom"] = "Wis"
        };

        if (aliases.TryGetValue(normalized, out string? label))
            return Fields.FirstOrDefault(field => field.Label.Equals(label, StringComparison.OrdinalIgnoreCase));

        return Fields.FirstOrDefault(field =>
            field.Label.Equals(key, StringComparison.OrdinalIgnoreCase) ||
            field.IniKey.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static string Clean(object? value)
    {
        return value?.ToString()?.Replace("\r", " ").Replace("\n", " ").Trim() ?? "";
    }

    private static bool IsTruthy(object? value)
    {
        if (value is null)
            return false;
        if (value is bool boolValue)
            return boolValue;
        if (value is int intValue)
            return intValue != 0;

        string text = Clean(value).ToLowerInvariant();
        return text is "1" or "true" or "yes" or "y" or "x" or "checked" or "on";
    }

    private static int ToInt(object? value)
    {
        string text = Clean(value);
        return int.TryParse(text, out int result) ? result : 0;
    }
}
