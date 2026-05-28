using RPGWO.Formats.Ini;

namespace RPGWO.Formats.Tests.Ini;

public sealed class IniParserTests
{
    [Fact]
    public void ParseText_ParsesBlankLines()
    {
        IniParseResult result = IniParser.ParseText("Name=Sword\n\nBurden=50");

        Assert.Equal(3, result.Document.LineCount);
        Assert.Equal(1, result.Document.BlankCount);
    }

    [Fact]
    public void ParseText_ParsesSemicolonComments()
    {
        IniParseResult result = IniParser.ParseText("; this is a comment");

        Assert.Single(result.Document.CommentLines);

        IniCommentLine comment = result.Document.CommentLines.Single();
        Assert.Equal(';', comment.Marker);
        Assert.Equal(" this is a comment", comment.Text);
    }

    [Fact]
    public void ParseText_ParsesApostropheComments()
    {
        IniParseResult result = IniParser.ParseText("' old VB style comment");

        Assert.Single(result.Document.CommentLines);

        IniCommentLine comment = result.Document.CommentLines.Single();
        Assert.Equal('\'', comment.Marker);
        Assert.Equal(" old VB style comment", comment.Text);
    }

    [Fact]
    public void ParseText_ParsesSectionLines()
    {
        IniParseResult result = IniParser.ParseText("[Items]");

        Assert.Single(result.Document.SectionLines);

        IniSectionLine section = result.Document.SectionLines.Single();
        Assert.Equal("Items", section.Name);
    }

    [Fact]
    public void ParseText_ParsesKeyValueLines()
    {
        IniParseResult result = IniParser.ParseText("Name=Iron Sword");

        Assert.Single(result.Document.KeyValueLines);

        IniKeyValueLine keyValue = result.Document.KeyValueLines.Single();
        Assert.Equal("Name", keyValue.Key);
        Assert.Equal("Iron Sword", keyValue.Value);
    }

    [Fact]
    public void ParseText_PreservesDuplicateKeys()
    {
        string text =
            "Animation0=100\n" +
            "Animation0=101\n" +
            "Animation0=102";

        IniParseResult result = IniParser.ParseText(text);

        IReadOnlyList<IniKeyValueLine> values = result.Document
            .GetValues("Animation0")
            .ToList();

        Assert.Equal(3, values.Count);
        Assert.Equal("100", values[0].Value);
        Assert.Equal("101", values[1].Value);
        Assert.Equal("102", values[2].Value);
    }
    [Fact]
    public void ParseText_CountsFlagLines()
    {
        string text =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Stackable\n" +
            "NotMovable";

        IniParseResult result = IniParser.ParseText(text);

        Assert.Equal(2, result.Document.FlagCount);
    }
    [Fact]
    public void ParseText_ParsesBareLinesAsFlags()
    {
        IniParseResult result = IniParser.ParseText("SomeWeirdFlag");

        Assert.Single(result.Document.FlagLines);

        IniFlagLine flag = result.Document.FlagLines.Single();
        Assert.Equal("SomeWeirdFlag", flag.Name);
        Assert.Equal("SomeWeirdFlag", flag.OriginalText);
    }

    [Fact]
    public void ParseText_WarnsWhenKeyIsEmpty()
    {
        IniParseResult result = IniParser.ParseText("=BadLine");

        Assert.True(result.HasWarnings);
        Assert.Contains(result.Warnings, warning => warning.Contains("empty key"));
    }

    [Fact]
    public void ParseText_TrimsKeysByDefault()
    {
        IniParseResult result = IniParser.ParseText(" Name =Iron Sword");

        IniKeyValueLine keyValue = result.Document.KeyValueLines.Single();

        Assert.Equal("Name", keyValue.Key);
        Assert.Equal("Iron Sword", keyValue.Value);
    }

    [Fact]
    public void ParseText_DoesNotTrimValuesByDefault()
    {
        IniParseResult result = IniParser.ParseText("Name= Iron Sword ");

        IniKeyValueLine keyValue = result.Document.KeyValueLines.Single();

        Assert.Equal("Name", keyValue.Key);
        Assert.Equal(" Iron Sword ", keyValue.Value);
    }
}