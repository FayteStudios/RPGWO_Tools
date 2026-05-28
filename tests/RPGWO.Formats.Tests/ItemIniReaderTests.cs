using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;

namespace RPGWO.Formats.Tests.Rpgwo;

public sealed class ItemIniReaderTests
{
    [Fact]
    public void ReadDocument_ReadsItemWithItemMarker()
    {
        string text =
            "Item=1\n" +
            "Name=Iron Sword\n" +
            "Image=123\n" +
            "Burden=50\n" +
            "Value=25";

        IniDocument document = IniParser.ParseText(text).Document;

        ItemIniReadResult result = ItemIniReader.ReadDocument(document);

        Assert.Single(result.Items);

        var item = result.Items[0];

        Assert.Equal(1, item.Id);
        Assert.Equal("Iron Sword", item.Name);
        Assert.Equal(123, item.Image);
        Assert.Equal(50, item.Burden);
        Assert.Equal(25, item.Value);
    }

    [Fact]
    public void ReadDocument_PreservesUnknownFields()
    {
        string text =
            "Item=1\n" +
            "Name=Iron Sword\n" +
            "SomeCustomField=abc";

        IniDocument document = IniParser.ParseText(text).Document;

        ItemIniReadResult result = ItemIniReader.ReadDocument(document);

        Assert.Single(result.Items);
        Assert.Single(result.Items[0].UnknownFields);

        Assert.Equal("SomeCustomField", result.Items[0].UnknownFields[0].Key);
        Assert.Equal("abc", result.Items[0].UnknownFields[0].Value);
    }

    [Fact]
    public void ReadDocument_ReadsMultipleItemsWithMarkers()
    {
        string text =
            "Item=1\n" +
            "Name=Iron Sword\n" +
            "Item=2\n" +
            "Name=Apple";

        IniDocument document = IniParser.ParseText(text).Document;

        ItemIniReadResult result = ItemIniReader.ReadDocument(document);

        Assert.Equal(2, result.Items.Count);

        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal("Iron Sword", result.Items[0].Name);

        Assert.Equal(2, result.Items[1].Id);
        Assert.Equal("Apple", result.Items[1].Name);
    }

    [Fact]
    public void ReadDocument_ReportsInvalidInteger()
    {
        string text =
            "Item=1\n" +
            "Name=Iron Sword\n" +
            "Image=abc";

        IniDocument document = IniParser.ParseText(text).Document;

        ItemIniReadResult result = ItemIniReader.ReadDocument(document);

        Assert.Single(result.Items);
        Assert.True(result.HasIssues);
        Assert.Null(result.Items[0].Image);
    }
    [Fact]
    public void ReadDocument_StoresGlobalFieldsBeforeFirstItem()
    {
        string text =
            "DefaultWeaponDurability=1000\n" +
            "DefaultArmorDurability=1000\n" +
            "Item=100\n" +
            "Name=Dandelion Seeds";

        IniDocument document = IniParser.ParseText(text).Document;

        ItemIniReadResult result = ItemIniReader.ReadDocument(document);

        Assert.Equal(2, result.GlobalFields.Count);
        Assert.Single(result.Items);

        Assert.Equal("DefaultWeaponDurability", result.GlobalFields[0].Key);
        Assert.Equal("1000", result.GlobalFields[0].Value);

        Assert.Equal(100, result.Items[0].Id);
        Assert.Equal("Dandelion Seeds", result.Items[0].Name);
    }

    [Fact]
    public void ReadDocument_AttachesBareFlagsToCurrentItem()
    {
        string text =
            "Item=100\n" +
            "Name=Dandelion Seeds\n" +
            "Stackable\n" +
            "NotMovable";

        IniDocument document = IniParser.ParseText(text).Document;

        ItemIniReadResult result = ItemIniReader.ReadDocument(document);

        Assert.Single(result.Items);

        var item = result.Items[0];

        Assert.Equal(2, item.Flags.Count);
        Assert.Contains("Stackable", item.Flags);
        Assert.Contains("NotMovable", item.Flags);
        Assert.True(item.NotMovable);
    }

    [Fact]
    public void ReadDocument_DoesNotCreateFakeItemFromGlobalFields()
    {
        string text =
            "DefaultWeaponDurability=1000\n" +
            "DefaultArmorDurability=1000\n" +
            "DefaultFireCatch=10";

        IniDocument document = IniParser.ParseText(text).Document;

        ItemIniReadResult result = ItemIniReader.ReadDocument(document);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.GlobalFields.Count);
    }
    [Fact]
    public void ReadDocument_ReadsBooleanFields()
    {
        string text =
            "Item=1\n" +
            "Name=Iron Sword\n" +
            "NotMovable=1\n" +
            "NoDrop=no";

        IniDocument document = IniParser.ParseText(text).Document;

        ItemIniReadResult result = ItemIniReader.ReadDocument(document);

        Assert.Single(result.Items);
        Assert.True(result.Items[0].NotMovable);
        Assert.False(result.Items[0].NoDrop);
    }
}