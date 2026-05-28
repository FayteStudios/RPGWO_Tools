namespace RPGWO.ServerTool.Models;

public sealed class SoundRow
{
    public string FileName { get; set; } = "";

    public string FullPath { get; set; } = "";

    public long SizeBytes { get; set; }

    public string SizeText
    {
        get
        {
            if (SizeBytes >= 1024 * 1024)
                return $"{SizeBytes / 1024d / 1024d:0.##} MB";

            if (SizeBytes >= 1024)
                return $"{SizeBytes / 1024d:0.##} KB";

            return $"{SizeBytes} B";
        }
    }
}