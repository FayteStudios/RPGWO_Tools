namespace RPGWO.Data.Definitions;

/// <summary>
/// Strongly typed representation of one animation.ini block.
/// animation.ini uses Animation=&lt;id&gt; as the start of each animation definition.
/// </summary>
public sealed class AnimationDefinition : DefinitionBase
{
    public List<string> Flags { get; } = new();

    public List<int> Frames { get; } = new();

    public List<int> FrameSizes { get; } = new();

    public List<string> Sounds { get; } = new();

    public bool? Rotational { get; set; }
}