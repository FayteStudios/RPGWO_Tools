public sealed class IniOption
{
    public required string Name { get; init; }
    public required string Kind { get; init; } // Field or Flag
    public string Description { get; init; } = "";
}