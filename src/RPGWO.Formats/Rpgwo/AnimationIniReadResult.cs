using RPGWO.Data.Common;
using RPGWO.Data.Definitions;
using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Rpgwo;

/// <summary>
/// Result from reading animation.ini into typed animation definitions.
/// </summary>
public sealed class AnimationIniReadResult
{
    public AnimationIniReadResult(IniDocument document)
    {
        Document = document;
    }

    public IniDocument Document { get; }

    public List<AnimationDefinition> Animations { get; } = new();

    public List<UnknownField> GlobalFields { get; } = new();

    public List<string> GlobalFlags { get; } = new();

    public List<DefinitionParseIssue> Issues { get; } = new();

    public bool HasIssues => Issues.Count > 0;

    public int UnknownFieldCount => Animations.Sum(animation => animation.UnknownFields.Count);

    public int FlagCount => Animations.Sum(animation => animation.Flags.Count);

    public int FrameCount => Animations.Sum(animation => animation.Frames.Count);

    public int FrameSizeCount => Animations.Sum(animation => animation.FrameSizes.Count);

    public int SoundCount => Animations.Sum(animation => animation.Sounds.Count);
}