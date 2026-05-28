using RPGWO.ServerTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RPGWO.ServerTool.Services;

public sealed class ItemEditorService
{
    private sealed record FieldDef(
        string Label,
        string IniKey,
        string Kind,
        string Group,
        bool Required = false,
        string DefaultValue = "",
        IReadOnlyList<string>? Options = null,
        string? RepeatSeparator = null);

    private sealed class ItemRecord
    {
        public Dictionary<string, object?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int Id => ToInt(Values.GetValueOrDefault("ID"));
    }

    private static readonly string[] SizeOptions =
    {
        "Tiny", "Small", "Medium", "Large", "Extra Large"
    };

    private static readonly string[] ClassOptions =
    {
        "Normal", "Plant", "Ore", "Ammo", "Wall", "Armor", "Weapon", "Wand", "Food", "Money",
        "Container", "Shield", "Bridge", "Trap", "Vendor", "JewelryNeck", "JewelryWrist", "JewelryFinger"
    };

    private static readonly string[] YesNoOptions = { "True", "False" };

    private static FieldDef Text(string label, string group = "General", string? iniKey = null, string defaultValue = "", bool required = false)
        => new(label, iniKey ?? label, "text", group, required, defaultValue);

    private static FieldDef Number(string label, string group = "General", string? iniKey = null, string defaultValue = "")
        => new(label, iniKey ?? label, "number", group, false, defaultValue);

    private static FieldDef Int(string label, string group = "General", string? iniKey = null, string defaultValue = "", bool required = false)
        => new(label, iniKey ?? label, "int", group, required, defaultValue);

    private static FieldDef Combo(string label, string group, IReadOnlyList<string> options, string? iniKey = null, string defaultValue = "", bool required = false)
        => new(label, iniKey ?? label, "combo", group, required, defaultValue, options);

    private static FieldDef Flag(string label, string group = "General", string? iniKey = null)
        => new(label, iniKey ?? label, "flag", group);

    private static FieldDef RepeatText(string label, string group, string separator)
        => new(label, label, "text", group, false, "", null, separator);

    private static readonly IReadOnlyList<FieldDef> Fields = new List<FieldDef>
    {
        Int("ID", "Base", "Item", required: true),
        Text("Name", "Base", required: true),

        Combo("Class", "General", ClassOptions, defaultValue: "Normal", required: true),
        Text("SubType", "General"),
        Combo("Size", "General", SizeOptions, defaultValue: "Small"),
        Number("Burden", "General", defaultValue: "0"),
        Number("Group", "General"),
        Number("Rarity", "General"),
        Number("TotalUses", "General"),
        Number("Value", "General", defaultValue: "0"),
        Flag("Stackable", "General"),

        Number("BreakDurability", "Equipment"),
        Number("BreakID", "Equipment"),
        Text("CombatSkill", "Equipment"),
        Number("SkillReq", "Equipment"),

        Number("Animation0", "Animations", defaultValue: "0"),
        Number("Animation1", "Animations"),
        Number("Animation2", "Animations"),
        Number("Animation3", "Animations"),
        Number("Animation4", "Animations"),
        Number("Animation5", "Animations"),
        Number("Animation6", "Animations"),
        Number("Animation7", "Animations"),
        Number("Animation8", "Animations"),
        Number("Animation9", "Animations"),
        Text("ImageType", "Animations"),
        Number("Playerimageoncarry", "Animations"),
        Number("WearImage", "Animations"),
        Text("DefaultAttackAnimation", "Animations"),
        Text("AttackAnimation", "Animations"),
        Text("ProjectileAnimation", "Animations"),

        Number("ArmorDurability", "Armor"),
        Number("ArmorLevel", "Armor"),
        Text("ArmorSpot", "Armor"),
        Number("BashAl", "Armor"),
        Number("ThrustAl", "Armor"),
        Number("FireAL", "Armor"),
        Number("ColdAL", "Armor"),
        Number("CutAl", "Armor"),
        Number("ElectricAL", "Armor"),
        Number("DefaultArmorDurability", "Armor"),
        Number("MagicArmorLevel", "Armor"),

        Number("Ammo", "Weapon"),
        Number("AttackSpeed", "Weapon"),
        Number("Blood", "Weapon"),
        Number("CriticalBonus", "Weapon"),
        Number("DamageHigh", "Weapon"),
        Number("DamageLow", "Weapon"),
        Number("DefaultWeaponDurability", "Weapon"),
        Number("EssenceSteal", "Weapon"),
        Number("MagicBreakChance", "Weapon"),
        Number("MagicBreakDamage", "Weapon"),
        Number("MagicBreakItemID", "Weapon"),
        Number("MagicPower", "Weapon"),
        Number("MagicStability", "Weapon"),
        Combo("MissleWeapon", "Weapon", YesNoOptions),
        Number("PoisonDamage", "Weapon"),
        Number("ThrowRange", "Weapon"),
        Text("ThrowSkill", "Weapon"),
        Number("ThrowSpawn", "Weapon"),
        Number("ThrowSpawnQty", "Weapon"),
        Number("WeaponAL", "Weapon"),
        Text("WeaponDamageType", "Weapon"),
        Number("WeaponDurability", "Weapon"),
        Number("WeaponMaxRange", "Weapon"),
        Number("WeaponMinRange", "Weapon"),
        Flag("2HandWeapon", "Weapon"),
        Flag("IgnoreArmor", "Weapon"),
        Flag("IgnoreShields", "Weapon"),
        Flag("PKDamage", "Weapon"),
        Flag("StaminaDamage", "Weapon"),
        Flag("Throwable", "Weapon"),

        Number("DefaultGrowthDeadItem", "Growth"),
        Number("GrowthDeadItem", "Growth"),
        Number("GrowthDelta", "Growth"),
        Number("DegradeDelta", "Growth"),
        Number("DegradeItem", "Growth"),
        Number("GrowthHighElevation", "Growth"),
        Number("GrowthItem", "Growth"),
        Number("GrowthLowElevation", "Growth"),

        Number("Food", "Food"),
        Number("FoodLife", "Food"),
        Number("FoodStamina", "Food"),
        Number("FoodMana", "Food"),
        Number("Water", "Food"),
        Number("Poison", "Food"),
        Number("PoisonCure", "Food"),

        Number("BonusCount", "Bonus"),
        Text("Data1", "Bonus"),
        Text("Data2", "Bonus"),
        Text("Data3", "Bonus"),
        Text("Data4", "Bonus"),
        Number("StrengthBonus", "Bonus"),
        Number("DexterityBonus", "Bonus"),
        Number("QuicknessBonus", "Bonus"),
        Number("IntelligenceBonus", "Bonus"),
        Number("WisdomBonus", "Bonus"),
        Number("SkillBonus", "Bonus"),
        Number("SkillIDBonus", "Bonus"),
        Text("Writing", "Bonus"),

        Number("StarterQty", "Skill"),
        RepeatText("StarterSkill", "Skill", ";"),

        Number("DefaultTraderMAX", "Trader"),
        Number("TraderMax", "Trader"),
        Flag("AlwaysStock", "Trader"),

        Number("SpawnMonster", "Mobs"),
        Number("SpawnMonsterChance", "Mobs"),
        Number("SpawnMonsterTimeout", "Mobs"),

        Text("FishDepth", "Proximity"),
        Number("RestGain", "Proximity"),
        Number("StandDamage", "Proximity"),
        Number("StepOnID", "Proximity"),
        Number("Warmth", "Proximity"),
        Number("WarmthRadius", "Proximity"),
        Number("FireCatch", "Proximity"),
        Number("ProximityChangeID", "Proximity"),
        Number("ProximityID", "Proximity"),
        Number("ProximityNegativeChangeID", "Proximity"),
        Number("ProximityRange", "Proximity"),
        Text("TrapEffect", "Proximity"),
        Number("TriggerId", "Proximity"),
        Number("HoldDamage", "Proximity"),
        Number("Light", "Proximity"),
        Text("MoveDirection", "Proximity"),
        Number("NiteID", "Proximity"),
        Number("Coolness", "Proximity"),
        Number("Bounce", "Proximity"),
        Number("DayID", "Proximity"),
        Number("DefaultFireCatch", "Proximity"),
        Flag("SpawnMonsterWhenPlayerNear", "Proximity"),

        Flag("Ancient", "Misc"),
        Flag("ApartmentMarker", "Misc"),
        Flag("ApartmentRenter", "Misc"),
        Flag("ctf", "Misc"),
        Flag("DeleteOnDeath", "Misc"),
        Flag("Destroyable", "Misc"),
        Flag("DoNotBlockPlayer", "Misc"),
        Flag("dropondeath", "Misc"),
        Flag("Equalizer", "Misc"),
        Flag("Explode", "Misc"),
        Flag("Fixable", "Misc"),
        Flag("InnDoor", "Misc"),
        Flag("InnKey", "Misc"),
        Flag("Invisible", "Misc"),
        Flag("Keyable", "Misc"),
        Flag("Lockable", "Misc"),
        Flag("NoDeathDrop", "Misc"),
        Flag("NoDrop", "Misc"),
        Flag("NoEconomyValueDrop", "Misc"),
        Flag("NotContainerable", "Misc"),
        Flag("NotMovable", "Misc"),
        Flag("NotPickupable", "Misc"),
        Flag("OneAllowed", "Misc"),
        Flag("OpenSightLine", "Misc"),
        Flag("Postable", "Misc"),
        Flag("Readable", "Misc"),
        Flag("Reflect", "Misc"),
        Flag("Refract", "Misc"),
        Flag("BlockMovement", "Misc"),
    };

    public IReadOnlyList<string> Groups { get; } = Fields.Select(field => field.Group).Distinct().ToList();

    public List<ItemRow> LoadItemRows(string itemIniPath)
    {
        return ParseItems(itemIniPath)
            .OrderBy(item => item.Id)
            .Select(ToRow)
            .ToList();
    }

    public List<ItemFieldRow> CreateBlankFieldRows(int nextId)
    {
        var values = BlankValues();
        values["ID"] = nextId.ToString();
        values["Name"] = "";
        return ToFieldRows(values);
    }

    public List<ItemFieldRow> LoadFieldRows(string itemIniPath, int itemId)
    {
        ItemRecord? record = ParseItems(itemIniPath).FirstOrDefault(item => item.Id == itemId);
        if (record is null)
            throw new InvalidOperationException($"Item={itemId} was not found.");

        return ToFieldRows(record.Values);
    }

    public string GetItemPreview(string itemIniPath, int itemId)
    {
        ItemRecord? record = ParseItems(itemIniPath).FirstOrDefault(item => item.Id == itemId);
        return record is null ? "" : string.Join(Environment.NewLine, ToIniLines(record));
    }

    public void SaveItemRows(string itemIniPath, IReadOnlyList<ItemFieldRow> fieldRows)
    {
        var record = FromFieldRows(fieldRows);
        if (record.Id <= 0)
            throw new InvalidOperationException("ID is required and must be a whole number greater than zero.");

        if (string.IsNullOrWhiteSpace(Clean(record.Values.GetValueOrDefault("Name"))))
            throw new InvalidOperationException("Name is required.");

        List<ItemRecord> items = File.Exists(itemIniPath) ? ParseItems(itemIniPath) : new List<ItemRecord>();
        items.RemoveAll(item => item.Id == record.Id);
        items.Add(record);
        WriteItems(itemIniPath, items.OrderBy(item => item.Id));
    }

    public void DeleteItem(string itemIniPath, int itemId)
    {
        List<ItemRecord> items = ParseItems(itemIniPath);
        items.RemoveAll(item => item.Id == itemId);
        WriteItems(itemIniPath, items.OrderBy(item => item.Id));
    }

    private static ItemRow ToRow(ItemRecord item) => new()
    {
        Id = item.Id,
        Name = Clean(item.Values.GetValueOrDefault("Name")),
        Class = Clean(item.Values.GetValueOrDefault("Class")),
        SubType = Clean(item.Values.GetValueOrDefault("SubType")),
        Animation0 = Clean(item.Values.GetValueOrDefault("Animation0")),
        Size = Clean(item.Values.GetValueOrDefault("Size")),
        Burden = Clean(item.Values.GetValueOrDefault("Burden")),
        Value = Clean(item.Values.GetValueOrDefault("Value"))
    };

    private static Dictionary<string, object?> BlankValues()
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (FieldDef field in Fields)
            values[field.Label] = field.Kind == "flag" ? false : field.DefaultValue;
        return values;
    }

    private static List<ItemFieldRow> ToFieldRows(Dictionary<string, object?> values)
    {
        return Fields.Select(field => new ItemFieldRow
        {
            Group = field.Group,
            Label = field.Label,
            IniKey = field.IniKey,
            Kind = field.Kind,
            Required = field.Required,
            IsCore = field.Group == "Base",
            Options = field.Options ?? Array.Empty<string>(),
            Enabled = field.Kind == "flag" && IsTruthy(values.GetValueOrDefault(field.Label)),
            Value = field.Kind == "flag" ? "" : Clean(values.GetValueOrDefault(field.Label))
        }).ToList();
    }

    private static ItemRecord FromFieldRows(IEnumerable<ItemFieldRow> rows)
    {
        var record = new ItemRecord();
        foreach (FieldDef field in Fields)
        {
            ItemFieldRow? row = rows.FirstOrDefault(candidate => string.Equals(candidate.Label, field.Label, StringComparison.OrdinalIgnoreCase));
            if (row is null)
                continue;

            record.Values[field.Label] = field.Kind == "flag" ? row.Enabled : Clean(row.Value);
        }
        return record;
    }

    private static List<ItemRecord> ParseItems(string path)
    {
        var items = new List<ItemRecord>();
        var current = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        void Flush()
        {
            if (!current.TryGetValue("ID", out object? idValue) || string.IsNullOrWhiteSpace(Clean(idValue)))
            {
                current.Clear();
                return;
            }

            var values = BlankValues();
            foreach (KeyValuePair<string, object?> pair in current)
            {
                FieldDef? field = FieldForKey(pair.Key);
                if (field is null)
                    continue;

                if (field.Kind == "flag")
                {
                    values[field.Label] = pair.Value is bool boolValue ? boolValue : IsTruthy(pair.Value);
                    continue;
                }

                string text = Clean(pair.Value);
                if (field.RepeatSeparator is not null && !string.IsNullOrWhiteSpace(Clean(values[field.Label])))
                    values[field.Label] = Clean(values[field.Label]) + field.RepeatSeparator + text;
                else
                    values[field.Label] = text;
            }

            var record = new ItemRecord();
            foreach (KeyValuePair<string, object?> pair in values)
                record.Values[pair.Key] = pair.Value;

            if (record.Id > 0)
                items.Add(record);

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

                if (key.Equals("Item", StringComparison.OrdinalIgnoreCase))
                {
                    Flush();
                    current["ID"] = value;
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
        return items;
    }

    private static void StoreValue(Dictionary<string, object?> current, string key, object? value)
    {
        FieldDef? field = FieldForKey(key);
        string storageKey = field?.Label ?? key.Trim();

        if (field?.RepeatSeparator is not null && current.TryGetValue(storageKey, out object? existing) && !string.IsNullOrWhiteSpace(Clean(existing)))
            current[storageKey] = Clean(existing) + field.RepeatSeparator + Clean(value);
        else
            current[storageKey] = value;
    }

    private static void WriteItems(string itemIniPath, IEnumerable<ItemRecord> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(itemIniPath)!);
        string output = string.Join(Environment.NewLine + Environment.NewLine, items.Select(item => string.Join(Environment.NewLine, ToIniLines(item))));
        if (!string.IsNullOrWhiteSpace(output))
            output += Environment.NewLine;
        File.WriteAllText(itemIniPath, output);
    }

    private static IEnumerable<string> ToIniLines(ItemRecord item)
    {
        yield return $"Item={item.Id}";
        yield return $"Name={Clean(item.Values.GetValueOrDefault("Name"))}";

        var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ID", "Name" };

        string[] commonOrder =
        {
            "Class", "SubType", "Animation0", "Animation1", "Animation2", "Animation3", "Animation4", "Animation5",
            "Animation6", "Animation7", "Animation8", "Animation9", "Size", "Burden", "Group", "Value", "Stackable"
        };

        foreach (string label in commonOrder)
        {
            FieldDef? field = Fields.FirstOrDefault(candidate => candidate.Label.Equals(label, StringComparison.OrdinalIgnoreCase));
            if (field is not null)
            {
                foreach (string line in FieldToLines(item, field))
                    yield return line;
                written.Add(field.Label);
            }
        }

        foreach (FieldDef field in Fields)
        {
            if (written.Contains(field.Label))
                continue;

            foreach (string line in FieldToLines(item, field))
                yield return line;
        }
    }

    private static IEnumerable<string> FieldToLines(ItemRecord item, FieldDef field)
    {
        object? raw = item.Values.GetValueOrDefault(field.Label);

        if (field.Kind == "flag")
        {
            if (!IsTruthy(raw))
                yield break;

            yield return field.Label.Equals("BlockMovement", StringComparison.OrdinalIgnoreCase)
                ? "BlockMovement=1"
                : field.IniKey;
            yield break;
        }

        string text = Clean(raw);
        if (text.Length == 0)
            yield break;

        if (field.RepeatSeparator is not null)
        {
            foreach (string part in text.Split(field.RepeatSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                yield return $"{field.IniKey}={part}";
        }
        else
        {
            yield return $"{field.IniKey}={text}";
        }
    }

    private static FieldDef? FieldForKey(string key)
    {
        string normalized = key.Trim().ToLowerInvariant();
        Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["item"] = "ID",
            ["itemid"] = "ID",
            ["id"] = "ID",
            ["breadkdurability"] = "BreakDurability",
            ["combat skill"] = "CombatSkill",
            ["animations0"] = "Animation0",
            ["eletrical"] = "ElectricAL",
            ["weaponmixrange"] = "WeaponMinRange",
            ["spawnmaster"] = "SpawnMonster",
            ["onallowed"] = "OneAllowed",
            ["opensightline"] = "OpenSightLine",
            ["spawnmonsterwhenplayerwear"] = "SpawnMonsterWhenPlayerNear"
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
