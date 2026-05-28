namespace RPGWO.Data.Definitions;

/// <summary>
/// Strongly typed representation of one itemuse.ini block.
/// itemuse.ini uses a bare "Itemuse" line as the start of each usage block.
/// EntryId is assigned sequentially by the reader because legacy files do not appear
/// to include an explicit ID field.
/// </summary>
public sealed class UsageDefinition : DefinitionBase
{
    public List<string> Flags { get; } = new();

    // Core
    public string? ItemTool { get; set; }

    public string? ItemFocus { get; set; }

    public string? FocusSubType { get; set; }

    public int? ItemToolQuantity { get; set; }

    // Skill / requirements
    public string? Skill { get; set; }

    public int? SkillMin { get; set; }

    public int? SkillMax { get; set; }

    public int? SkillXpSuccess { get; set; }

    public int? SkillXpFailure { get; set; }

    public int? StaminaCost { get; set; }

    public int? Range { get; set; }

    public int? Animation { get; set; }

    // Success / failure results
    public string? SuccessTool { get; set; }

    public string? SuccessFocus { get; set; }

    public string? FailedTool { get; set; }

    public string? FailedFocus { get; set; }

    public int? FailedDamage { get; set; }

    public List<string> SuccessItems { get; } = new();

    public List<int> SuccessItemQuantities { get; } = new();

    public List<string> FailedItems { get; } = new();

    public List<int> FailedItemQuantities { get; } = new();

    // Surface / location
    public bool? NeedFlatSurface { get; set; }

    public bool? NeedUnLevelSurface { get; set; }

    public string? SurfaceGround { get; set; }

    public string? SurfaceUnderGround { get; set; }

    public string? SurfaceWater { get; set; }

    public bool? UsePlayerPosition { get; set; }

    // Messages
    public string? SuccessMessage { get; set; }

    public string? FailedMessage { get; set; }

    // Misc value fields
    public int? MonsterId { get; set; }

    public int? PlayerUsageTimeout { get; set; }

    public int? GiveSkillBonus { get; set; }

    public string? Guild { get; set; }

    public int? Drunk { get; set; }

    public int? Heal { get; set; }

    public int? HealPoison { get; set; }

    public string? Warp { get; set; }

    // Bare flags
    public bool? OwnLand { get; set; }

    public bool? PublicUse { get; set; }

    public bool? NotOnPlayer { get; set; }

    public bool? PreserveData { get; set; }

    public bool? Hidden { get; set; }

    public bool? SurfaceOnly { get; set; }

    public bool? UnderGroundOnly { get; set; }

    public bool? DigUnderGround { get; set; }

    public bool? LowerLand { get; set; }

    public bool? RaiseLand { get; set; }

    public bool? PlotUse { get; set; }

    public bool? LockFocus { get; set; }

    public bool? ResetArmor { get; set; }

    public bool? ResetWeapon { get; set; }

    public bool? ResetItemUse { get; set; }

    public bool? UseAllQuantity { get; set; }

    public bool? UseItemSkillReq { get; set; }

    public bool? ReverseTool { get; set; }

    public bool? SetFocusData8 { get; set; }

    public bool? SetWriting { get; set; }

    public bool? ShowWriting { get; set; }

    public bool? DisplayKeyFocus { get; set; }

    public bool? KeyFocus { get; set; }

    public bool? Picklock { get; set; }

    public bool? DisarmTrap { get; set; }

    public bool? MakePk { get; set; }

    public bool? MakeNonPk { get; set; }

    public bool? SetResurrectSpot { get; set; }

    public bool? RenewInnRoom { get; set; }

    // Extra core / subtype quantities.
    public string? ToolSubType { get; set; }

    public int? ItemFocusQuantity { get; set; }

    // Additional skill requirements.
    public string? Skill2 { get; set; }

    public int? Mana { get; set; }

    // Build / construction.
    public string? BuildItem { get; set; }

    public int? BuildNeeded { get; set; }

    public int? BuildWork { get; set; }

    // Revive / special effects.
    public int? Revive { get; set; }

    // Focus data mutation.
    public int? SetFocusData1 { get; set; }

    // Key / lock data values. These can appear as key=value, not just bare flags.
    public string? KeyFocusValue { get; set; }

    public string? PicklockValue { get; set; }

    public string? ReverseToolValue { get; set; }

    // Additional bare flags found in real itemuse.ini.
    public bool? DoNotUseSuccessTool { get; set; }

    public bool? UseSuccessTool { get; set; }

    public bool? Trigger { get; set; }

    public bool? WarpFlag { get; set; }





}