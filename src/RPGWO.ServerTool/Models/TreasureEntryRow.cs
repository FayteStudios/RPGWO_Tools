namespace RPGWO.ServerTool.Models;

public sealed class TreasureEntryRow
{
    public int EntryIndex { get; set; }

    public string Item { get; set; } = "";

    public string SkillId { get; set; } = "";

    public string SkillLow { get; set; } = "";

    public string SkillHigh { get; set; } = "";

    public string Chance { get; set; } = "";

    public string SpellID { get; set; } = "";

    public string SpellData { get; set; } = "";

    public int FieldCount { get; init; }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Item))
                return Item;

            if (!string.IsNullOrWhiteSpace(SpellID))
                return $"Spell {SpellID}";

            return $"Entry {EntryIndex}";
        }
    }
}