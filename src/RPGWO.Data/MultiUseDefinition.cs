namespace RPGWO.Data.Definitions;

/// <summary>
/// Strongly typed representation of one multiuse.ini block.
/// multiuse.ini uses a bare "MultiUse" line as the start of each recipe block.
/// RecipeId is assigned sequentially by the reader because legacy files do not
/// appear to include an explicit recipe ID field.
/// </summary>
public sealed class MultiUseDefinition : DefinitionBase
{
    public List<string> Flags { get; } = new();

    // Core
    public string? SuccessItem { get; set; }

    public int? SuccessItemQuantity { get; set; }

    public string? FocusItem { get; set; }

    // Needed/result rows
    public List<string> NeedItems { get; } = new();

    public List<int> NeedItemQuantities { get; } = new();

    public List<string> ResultItems { get; } = new();

    public List<int> ResultItemQuantities { get; } = new();

    // Skill / XP
    public string? Skill { get; set; }

    public int? SkillMin { get; set; }

    public int? SkillMax { get; set; }

    public int? SkillXpSuccess { get; set; }

    public int? StaminaCost { get; set; }

    // Messages
    public string? SuccessMessage { get; set; }
}