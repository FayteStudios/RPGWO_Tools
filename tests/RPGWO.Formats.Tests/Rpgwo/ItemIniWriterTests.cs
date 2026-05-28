using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;

namespace RPGWO.Formats.Tests.Rpgwo;

public sealed class ItemIniWriterTests
{
    [Fact]
    public void SetItemField_UpdatesExistingField()
    {
        string text =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Value=5\n" +
            "Item=102\n" +
            "Name=Sprouting Dandelion";

        IniDocument document = IniParser.ParseText(text).Document;

        bool updated = ItemIniWriter.SetItemField(
            document,
            itemId: 100,
            fieldName: "Name",
            value: "Better Dandelion Seeds");

        Assert.True(updated);

        string output = IniWriter.WriteToString(document);

        Assert.Contains("Name=Better Dandelion Seeds", output);
        Assert.Contains("Name=Sprouting Dandelion", output);
    }

    [Fact]
    public void SetItemField_InsertsMissingFieldBeforeNextItem()
    {
        string text =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Item=102\n" +
            "Name=Sprouting Dandelion";

        IniDocument document = IniParser.ParseText(text).Document;

        bool updated = ItemIniWriter.SetItemField(
            document,
            itemId: 100,
            fieldName: "Value",
            value: "10");

        Assert.True(updated);

        string output = IniWriter.WriteToString(document);

        string expected =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Value=10\n" +
            "Item=102\n" +
            "Name=Sprouting Dandelion";

        Assert.Equal(expected, output);
    }

    [Fact]
    public void SetItemField_ReturnsFalseForMissingItem()
    {
        string text =
            "Item=100\n" +
            "Name=Dandelion Seeds";

        IniDocument document = IniParser.ParseText(text).Document;

        bool updated = ItemIniWriter.SetItemField(
            document,
            itemId: 999,
            fieldName: "Name",
            value: "Missing Item");

        Assert.False(updated);
    }

    [Fact]
    public void SetItemFlag_AddsMissingFlag()
    {
        string text =
            "Item=100\n" +
            "Name=Dandelion Seeds";

        IniDocument document = IniParser.ParseText(text).Document;

        bool updated = ItemIniWriter.SetItemFlag(
            document,
            itemId: 100,
            flagName: "Stackable",
            enabled: true);

        Assert.True(updated);

        string output = IniWriter.WriteToString(document);

        string expected =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Stackable";

        Assert.Equal(expected, output);
    }

    [Fact]
    public void SetItemFlag_DoesNotDuplicateExistingFlag()
    {
        string text =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Stackable";

        IniDocument document = IniParser.ParseText(text).Document;

        bool updated = ItemIniWriter.SetItemFlag(
            document,
            itemId: 100,
            flagName: "Stackable",
            enabled: true);

        Assert.True(updated);

        string output = IniWriter.WriteToString(document);

        Assert.Equal(text, output);
    }

    [Fact]
    public void SetItemFlag_RemovesExistingFlag()
    {
        string text =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Stackable\n" +
            "Item=102\n" +
            "Name=Sprouting Dandelion\n" +
            "Stackable";

        IniDocument document = IniParser.ParseText(text).Document;

        bool updated = ItemIniWriter.SetItemFlag(
            document,
            itemId: 100,
            flagName: "Stackable",
            enabled: false);

        Assert.True(updated);

        string output = IniWriter.WriteToString(document);

        string expected =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Item=102\n" +
            "Name=Sprouting Dandelion\n" +
            "Stackable";

        Assert.Equal(expected, output);
    }

    [Fact]
    public void RemoveItemField_RemovesOnlyFieldInsideTargetItem()
    {
        string text =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Food=1\n" +
            "Item=102\n" +
            "Name=Sprouting Dandelion\n" +
            "Food=1";

        IniDocument document = IniParser.ParseText(text).Document;

        bool removed = ItemIniWriter.RemoveItemField(
            document,
            itemId: 100,
            fieldName: "Food");

        Assert.True(removed);

        string output = IniWriter.WriteToString(document);

        string expected =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Item=102\n" +
            "Name=Sprouting Dandelion\n" +
            "Food=1";

        Assert.Equal(expected, output);
    }
}