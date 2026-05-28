using RPGWO.Data.Common;

namespace RPGWO.Data.Definitions;

/// <summary>
/// Base type for RPGWO definitions loaded from legacy data files.
/// Examples: items, monsters, spells, skills, animations.
/// </summary>
public abstract class DefinitionBase
{
    /// <summary>
    /// Numeric RPGWO definition ID.
    /// For item.ini this is usually taken from item section/order or explicit ID data later.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display or internal name, depending on the source file.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Unknown fields preserved from the source file.
    /// This lets us read old/custom servers without destroying data we do not understand yet.
    /// </summary>
    public List<UnknownField> UnknownFields { get; } = new();

    /// <summary>
    /// Non-fatal parse issues connected to this definition.
    /// </summary>
    public List<DefinitionParseIssue> Issues { get; } = new();
}