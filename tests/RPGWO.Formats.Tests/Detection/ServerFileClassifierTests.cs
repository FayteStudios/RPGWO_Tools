using RPGWO.Core;
using RPGWO.Formats.Detection;

namespace RPGWO.Formats.Tests.Detection;

public sealed class ServerFileClassifierTests
{
    [Theory]
    [InlineData("world.ini", ServerFileKind.WorldIni)]
    [InlineData("item.ini", ServerFileKind.ItemIni)]
    [InlineData("monster.ini", ServerFileKind.MonsterIni)]
    [InlineData("magic.ini", ServerFileKind.MagicIni)]
    [InlineData("skill.ini", ServerFileKind.SkillIni)]
    [InlineData("itemuse.ini", ServerFileKind.ItemUseIni)]
    [InlineData("multiuse.ini", ServerFileKind.MultiUseIni)]
    [InlineData("treasure.ini", ServerFileKind.TreasureIni)]
    [InlineData("animation.ini", ServerFileKind.AnimationIni)]
    [InlineData("waterside.ini", ServerFileKind.WaterSideIni)]
    [InlineData("underside.ini", ServerFileKind.UnderSideIni)]
    public void ClassifyPath_ClassifiesKnownIniFiles(
        string fileName,
        ServerFileKind expectedKind)
    {
        ServerFileKind actualKind = ServerFileClassifier.ClassifyPath(fileName);

        Assert.Equal(expectedKind, actualKind);
    }

    [Theory]
    [InlineData("world.dat", ServerFileKind.DataFile)]
    [InlineData("player.dat", ServerFileKind.DataFile)]
    [InlineData("sprites.bmp", ServerFileKind.SpriteSheet)]
    [InlineData("notes.txt", ServerFileKind.TextFile)]
    [InlineData("server.log", ServerFileKind.LogFile)]
    [InlineData("server.exe", ServerFileKind.Executable)]
    [InlineData("MSVBVM60.dll", ServerFileKind.Library)]
    [InlineData("MSWINSCK.ocx", ServerFileKind.Library)]
    public void ClassifyPath_ClassifiesKnownExtensions(
        string fileName,
        ServerFileKind expectedKind)
    {
        ServerFileKind actualKind = ServerFileClassifier.ClassifyPath(fileName);

        Assert.Equal(expectedKind, actualKind);
    }

    [Fact]
    public void ClassifyPath_IsCaseInsensitiveForKnownFiles()
    {
        ServerFileKind actualKind = ServerFileClassifier.ClassifyPath("ITEM.INI");

        Assert.Equal(ServerFileKind.ItemIni, actualKind);
    }

    [Fact]
    public void ClassifyPath_ReturnsUnknownForUnrecognizedFiles()
    {
        ServerFileKind actualKind = ServerFileClassifier.ClassifyPath("random.xyz");

        Assert.Equal(ServerFileKind.Unknown, actualKind);
    }

    [Theory]
    [InlineData("item.ini", true)]
    [InlineData("monster.ini", true)]
    [InlineData("animation.ini", true)]
    [InlineData("world.dat", false)]
    [InlineData("sprites.bmp", false)]
    [InlineData("random.xyz", false)]
    public void IsKnownRpgwoDefinitionFile_ReturnsExpectedResult(
        string fileName,
        bool expected)
    {
        bool actual = ServerFileClassifier.IsKnownRpgwoDefinitionFile(fileName);

        Assert.Equal(expected, actual);
    }
}