using RPGWO.ServerTool.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RPGWO.ServerTool.Services;

public sealed class SoundLibraryService
{
    public string GetSoundFolder(string serverFolderPath)
    {
        return Path.Combine(serverFolderPath, "Sounds");
    }

    public void EnsureSoundFolder(string serverFolderPath)
    {
        Directory.CreateDirectory(GetSoundFolder(serverFolderPath));
    }

    public List<SoundRow> LoadSounds(string serverFolderPath)
    {
        string soundFolder = GetSoundFolder(serverFolderPath);

        if (!Directory.Exists(soundFolder))
            return new List<SoundRow>();

        return Directory
            .EnumerateFiles(soundFolder, "*.wav", SearchOption.TopDirectoryOnly)
            .Select(path =>
            {
                FileInfo info = new(path);

                return new SoundRow
                {
                    FileName = info.Name,
                    FullPath = info.FullName,
                    SizeBytes = info.Length
                };
            })
            .OrderBy(sound => sound.FileName)
            .ToList();
    }
}