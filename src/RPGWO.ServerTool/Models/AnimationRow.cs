namespace RPGWO.ServerTool.Models;

/// <summary>
/// List row for one animation.ini block.
/// </summary>
public sealed class AnimationRow
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public int FrameCount { get; init; }

    public int SoundCount { get; init; }

    public bool Rotational { get; init; }

    public int UnknownCount { get; init; }

    public int IssueCount { get; init; }

    public string Summary =>
        $"Frames: {FrameCount}, Sounds: {SoundCount}, Rotational: {Rotational}, Unknowns: {UnknownCount}, Issues: {IssueCount}";
}