using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Tests.Ini;

public sealed class IniWriterTests
{
    [Fact]
    public void WriteToString_RoundTripsUnchangedText()
    {
        string text =
            "; comment\n" +
            "Name=Iron Sword\n" +
            "\n" +
            "Animation0=100\n" +
            "Animation0=101\n" +
            "SomeWeirdFlag";

        IniParseResult result = IniParser.ParseText(text);

        string output = IniWriter.WriteToString(result.Document);

        Assert.Equal(text, output);
    }

    [Fact]
    public void WriteToString_PreservesSpacingForUnmodifiedKeyValues()
    {
        string text = "Name = Iron Sword";

        IniParseResult result = IniParser.ParseText(text);

        string output = IniWriter.WriteToString(result.Document);

        Assert.Equal(text, output);
    }

    [Fact]
    public void WriteToString_WritesModifiedKeyValue()
    {
        string text = "Name=Iron Sword";

        IniParseResult result = IniParser.ParseText(text);

        IniKeyValueLine nameLine = result.Document.GetFirstValue("Name")
            ?? throw new InvalidOperationException("Expected Name line.");

        nameLine.Value = "Steel Sword";
        nameLine.IsModified = true;

        string output = IniWriter.WriteToString(result.Document);

        Assert.Equal("Name=Steel Sword", output);
    }

    [Fact]
    public void WriteToString_PreservesRawLines()
    {
        string text = "SomeWeirdFlag";

        IniParseResult result = IniParser.ParseText(text);

        string output = IniWriter.WriteToString(result.Document);

        Assert.Equal(text, output);
    }

    [Fact]
    public void WriteToString_PreservesCommentsAndBlankLines()
    {
        string text =
            "; comment\n" +
            "\n" +
            "' vb comment\n" +
            "Name=Sword";

        IniParseResult result = IniParser.ParseText(text);

        string output = IniWriter.WriteToString(result.Document);

        Assert.Equal(text, output);
    }
}