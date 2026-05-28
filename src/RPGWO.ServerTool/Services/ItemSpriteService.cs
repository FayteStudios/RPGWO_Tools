using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RPGWO.ServerTool.Services;

public sealed class ItemSpriteService
{
    public const int SpriteSize = 32;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".png", ".gif", ".jpg", ".jpeg"
    };

    public sealed record SpriteSheet(string Path, int SheetNumber, int Columns, int Rows)
    {
        public int FirstSpriteId => SheetNumber * 100;
        public int SpriteCount => Columns * Rows;
        public int LastSpriteId => FirstSpriteId + SpriteCount - 1;
        public string FileName => System.IO.Path.GetFileName(Path);
    }

    public IReadOnlyList<SpriteSheet> LoadItemSheets(string serverFolderPath)
    {
        string spriteFolder = System.IO.Path.Combine(serverFolderPath, "Sprites");
        if (!Directory.Exists(spriteFolder))
            return Array.Empty<SpriteSheet>();

        List<SpriteSheet> sheets = new();

        foreach (string path in Directory.EnumerateFiles(spriteFolder).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            if (!SupportedExtensions.Contains(System.IO.Path.GetExtension(path)))
                continue;

            string stem = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!stem.StartsWith("item", StringComparison.OrdinalIgnoreCase))
                continue;

            string numberText = stem[4..];
            if (!int.TryParse(numberText, out int sheetNumber))
                continue;

            try
            {
                BitmapImage image = LoadBitmap(path);
                int columns = image.PixelWidth / SpriteSize;
                int rows = image.PixelHeight / SpriteSize;

                if (columns <= 0 || rows <= 0)
                    continue;

                sheets.Add(new SpriteSheet(path, sheetNumber, columns, rows));
            }
            catch
            {
                // Keep sprite loading simple: skip unreadable sheets instead of blocking item editing.
            }
        }

        return sheets.OrderBy(sheet => sheet.SheetNumber).ToList();
    }

    public ImageSource? GetItemSpriteImage(string serverFolderPath, string? spriteIdText)
    {
        if (!TryParseInt(spriteIdText, out int spriteId))
            return null;

        SpriteSheet? sheet = LoadItemSheets(serverFolderPath)
            .FirstOrDefault(value => value.FirstSpriteId <= spriteId && spriteId <= value.LastSpriteId);

        if (sheet is null)
            return null;

        return CropSprite(sheet, spriteId);
    }

    public ImageSource? GetItemCompositeImage(string serverFolderPath, string? animation0Text, string? imageTypeText)
    {
        if (!TryParseInt(animation0Text, out int animation0))
            return null;

        int imageType = TryParseInt(imageTypeText, out int parsedImageType) ? parsedImageType : 0;
        List<int> ids = CompositeIds(animation0, imageType).ToList();
        List<ImageSource> tiles = ids
            .Select(id => GetItemSpriteImage(serverFolderPath, id.ToString()))
            .Where(image => image is not null)
            .Cast<ImageSource>()
            .ToList();

        if (tiles.Count == 0)
            return null;

        if (imageType == 1)
            return Compose(tiles, 1, 2);

        if (imageType == 2)
            return Compose(tiles, 2, 2);

        return tiles[0];
    }

    public IReadOnlyList<int> CompositeIds(int animation0, int imageType)
    {
        return imageType switch
        {
            1 => new[] { animation0, animation0 + 10 },
            2 => new[] { animation0, animation0 + 1, animation0 + 10, animation0 + 11 },
            _ => new[] { animation0 }
        };
    }

    public ImageSource? CropSprite(SpriteSheet sheet, int spriteId)
    {
        int index = spriteId - sheet.FirstSpriteId;
        if (index < 0 || index >= sheet.SpriteCount)
            return null;

        BitmapImage image = LoadBitmap(sheet.Path);
        int column = index % sheet.Columns;
        int row = index / sheet.Columns;

        return new CroppedBitmap(
            image,
            new Int32Rect(column * SpriteSize, row * SpriteSize, SpriteSize, SpriteSize));
    }

    private static ImageSource Compose(IReadOnlyList<ImageSource> tiles, int columns, int rows)
    {
        DrawingVisual visual = new();

        using (DrawingContext context = visual.RenderOpen())
        {
            for (int index = 0; index < tiles.Count; index++)
            {
                int x = index % columns;
                int y = index / columns;
                if (y >= rows)
                    break;

                context.DrawImage(tiles[index], new Rect(x * SpriteSize, y * SpriteSize, SpriteSize, SpriteSize));
            }
        }

        RenderTargetBitmap bitmap = new(
            columns * SpriteSize,
            rows * SpriteSize,
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapImage LoadBitmap(string path)
    {
        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static bool TryParseInt(string? text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return int.TryParse(text.Trim(), out value);
    }
}
