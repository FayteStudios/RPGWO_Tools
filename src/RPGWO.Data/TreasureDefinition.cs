namespace RPGWO.Data.Definitions;

/// <summary>
/// Strongly typed representation of one treasure table from treasure.ini.
/// treasure.ini is expected to use Treasure=&lt;id&gt; as the start of each treasure definition.
/// </summary>
public sealed class TreasureDefinition : DefinitionBase
{
    public List<string> Flags { get; } = new();

    // Core table behavior.
    public int? TreasureCount { get; set; }

    public int? Gold { get; set; }

    public int? GoldMin { get; set; }

    public int? GoldMax { get; set; }

    public int? Money { get; set; }

    public int? MoneyMin { get; set; }

    public int? MoneyMax { get; set; }

    public double? Chance { get; set; }

    public int? Quantity { get; set; }

    public int? QuantityMin { get; set; }

    public int? QuantityMax { get; set; }

    // Numbered item rows.
    public List<string> Items { get; } = new();

    public List<int> ItemQuantities { get; } = new();

    public List<int> ItemQuantityMins { get; } = new();

    public List<int> ItemQuantityMaxes { get; } = new();

    public List<double> ItemChances { get; } = new();

    public List<string> ItemData1 { get; } = new();

    public List<string> ItemData2 { get; } = new();

    public List<string> ItemData3 { get; } = new();

    public List<string> ItemData4 { get; } = new();

    public List<string> ItemTexts { get; } = new();

    public List<int> ItemTotalUses { get; } = new();

    // Numbered group/category rows.
    public List<string> Groups { get; } = new();

    public List<double> GroupChances { get; } = new();

    public List<int> GroupQuantities { get; } = new();

    public List<string> Catagories { get; } = new();

    public List<double> CatagoryChances { get; } = new();

    public List<int> CatagoryQuantities { get; } = new();

    // Nested treasure table references.
    public List<int> TreasureRefs { get; } = new();

    public List<double> TreasureRefChances { get; } = new();

    public List<int> TreasureRefQuantities { get; } = new();

    // Common flags / behavior switches.
    public bool? NoDrop { get; set; }

    public bool? Random { get; set; }

    public bool? Always { get; set; }

    public bool? OneOnly { get; set; }

    public bool? NoEconomyValueDrop { get; set; }


    // Legacy treasure table name from Treasure=<name>.
    public string? TreasureName { get; set; }

    // Repeated skill/generation rows.
    public List<string> SkillIds { get; } = new();

    public List<int> SkillLows { get; } = new();

    public List<int> SkillHighs { get; } = new();

    // Repeated spell rows.
    public List<string> SpellIds { get; } = new();

    public List<string> SpellData { get; } = new();

    // Cost appears once per named treasure block in this file.
    public int? Cost { get; set; }
}