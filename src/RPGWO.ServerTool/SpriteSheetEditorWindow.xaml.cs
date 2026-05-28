using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RPGWO_INI_Tools
{
    public partial class SpriteSheetEditorWindow : Window
    {
        private const int SpriteSize = 32;
        private const int OutputColumns = 10;
        private const int OutputRows = 10;
        private const int MaxOutputSprites = OutputColumns * OutputRows;

        private const int SourceSheetColumns = 10;
        private const int SpriteDisplayScale = 2;
        private const int SpriteDisplaySize = SpriteSize * SpriteDisplayScale;
        private const int SpriteButtonSize = SpriteDisplaySize + 6;

        private static readonly string AppDir = FindProjectRootDirectory();
        private static readonly string SpriteDir = Path.Combine(AppDir, "Sprites");
        private static readonly string SpriteArchiveDir = Path.Combine(AppDir, "Sprites Archive");

        private static readonly Regex SpriteSheetPattern =
            new Regex(@"^(item|player)(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly List<SpriteSheetEntry> _entries = new();
        private readonly ObservableCollection<OutputSpriteSlot> _outputSlots = new();

        private SpriteSheetEntry? _currentEntry;
        private OutputSpriteSlot? _selectedOutputSlot;


        public SpriteSheetEditorWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            for (int i = 0; i < MaxOutputSprites; i++)
            {
                _outputSlots.Add(new OutputSpriteSlot
                {
                    SlotIndex = i
                });
            }

            Directory.CreateDirectory(SpriteDir);
            Directory.CreateDirectory(SpriteArchiveDir);

            LoadLibrary();
            RefreshTypes();
            RefreshSourceButtons();
            RefreshQueuePreview();

            StatusText.Text = $"Loaded {_entries.Count} archived sprite sheet(s) from {SpriteArchiveDir}.";
        }

        private static string FindProjectRootDirectory()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir != null)
            {
                bool looksLikeProjectRoot =
                    File.Exists(Path.Combine(dir.FullName, "RPGWO.ServerTool.csproj")) ||
                    Directory.Exists(Path.Combine(dir.FullName, "Sprites")) ||
                    Directory.Exists(Path.Combine(dir.FullName, "Sprites Archive"));

                bool isBuildOutput =
                    dir.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                    dir.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

                if (looksLikeProjectRoot && !isBuildOutput)
                    return dir.FullName;

                dir = dir.Parent;
            }

            // Fallback for published exe builds:
            // put Sprites and Sprites Archive beside the .exe.
            return AppContext.BaseDirectory;
        }

        private void LoadLibrary()
        {
            _entries.Clear();

            Directory.CreateDirectory(SpriteDir);
            Directory.CreateDirectory(SpriteArchiveDir);

            string[] supportedExtensions =
            {
        ".bmp", ".png", ".gif", ".jpg", ".jpeg"
    };

            int supportedImageCount = 0;
            int matchingNameCount = 0;
            int loadedCount = 0;

            List<string> skipped = new();

            foreach (string file in Directory.EnumerateFiles(SpriteArchiveDir))
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();

                if (!supportedExtensions.Contains(extension))
                    continue;

                supportedImageCount++;

                string nameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                Match match = SpriteSheetPattern.Match(nameWithoutExtension);

                if (!match.Success)
                {
                    skipped.Add($"{Path.GetFileName(file)} skipped: name must be item<number> or player<number>.");
                    continue;
                }

                matchingNameCount++;

                string prefix = match.Groups[1].Value.ToLowerInvariant();

                if (!int.TryParse(match.Groups[2].Value, out int sheetNumber))
                {
                    skipped.Add($"{Path.GetFileName(file)} skipped: could not parse sheet number.");
                    continue;
                }

                BitmapSource? image = TryLoadBitmapSource(file);

                if (image == null)
                {
                    skipped.Add($"{Path.GetFileName(file)} skipped: WPF could not load image.");
                    continue;
                }

                int columns = image.PixelWidth / SpriteSize;
                int rows = image.PixelHeight / SpriteSize;

                if (columns <= 0 || rows <= 0)
                {
                    skipped.Add($"{Path.GetFileName(file)} skipped: image is smaller than {SpriteSize}x{SpriteSize}.");
                    continue;
                }

                if (image.PixelWidth % SpriteSize != 0 || image.PixelHeight % SpriteSize != 0)
                {
                    skipped.Add($"{Path.GetFileName(file)} warning: size {image.PixelWidth}x{image.PixelHeight} is not evenly divisible by {SpriteSize}.");
                }

                _entries.Add(new SpriteSheetEntry
                {
                    Path = file,
                    SheetPrefix = prefix,
                    SheetNumber = sheetNumber,
                    Columns = columns,
                    Rows = rows,
                    Image = image
                });

                loadedCount++;
            }

            _entries.Sort((a, b) =>
            {
                int typeCompare = string.Compare(a.SheetPrefix, b.SheetPrefix, StringComparison.OrdinalIgnoreCase);

                if (typeCompare != 0)
                    return typeCompare;

                int numberCompare = a.SheetNumber.CompareTo(b.SheetNumber);

                if (numberCompare != 0)
                    return numberCompare;

                return string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
            });

            string message =
                $"Scanned:\n{SpriteArchiveDir}\n\n" +
                $"Supported image files found: {supportedImageCount}\n" +
                $"Matching item/player sheets: {matchingNameCount}\n" +
                $"Loaded sheets: {loadedCount}";

            if (skipped.Count > 0)
            {
                message += "\n\nNotes:\n" + string.Join("\n", skipped.Take(8));

                if (skipped.Count > 8)
                    message += $"\n...and {skipped.Count - 8} more.";
            }

            StatusText.Text = $"Loaded {_entries.Count} archived sprite sheet(s) from {SpriteArchiveDir}.";
        }
        private static BitmapSource? TryLoadBitmapSource(string path)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                try
                {
                    using FileStream stream = File.OpenRead(path);

                    BitmapDecoder decoder = BitmapDecoder.Create(
                        stream,
                        BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.OnLoad
                    );

                    BitmapSource frame = decoder.Frames[0];
                    frame.Freeze();

                    return frame;
                }
                catch
                {
                    return null;
                }
            }
        }
        private void ReloadSpriteArchive_Click(object sender, RoutedEventArgs e)
        {
            LoadLibrary();
            RefreshTypes();
            RefreshSourceButtons();

            if (_currentEntry != null)
            {
                _currentEntry = _entries.FirstOrDefault(e2 => e2.SourceId == _currentEntry.SourceId);
                if (_currentEntry != null)
                {
                    OpenSheet(_currentEntry);
                }
                else
                {
                    SpriteGridPanel.Children.Clear();
                    SheetInfoText.Text = "No sheet selected.";
                }
            }

            RefreshQueuePreview();
            StatusText.Text = $"Loaded {_entries.Count} archived sprite sheet(s) from {SpriteArchiveDir}.";
        }

        private void RefreshTypes()
        {
            string current = TypeCombo.SelectedItem as string ?? "All";

            List<string> types = _entries
                .Select(e => e.SheetPrefix)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v)
                .ToList();

            types.Insert(0, "All");

            TypeCombo.ItemsSource = types;

            if (types.Contains(current))
                TypeCombo.SelectedItem = current;
            else
                TypeCombo.SelectedIndex = 0;
        }

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            RefreshSourceButtons();
        }

        private void RefreshSourceButtons()
        {
            SourceButtonsPanel.Children.Clear();

            string selectedType = TypeCombo.SelectedItem as string ?? "All";

            IEnumerable<SpriteSheetEntry> visibleEntries = _entries;

            if (!string.Equals(selectedType, "All", StringComparison.OrdinalIgnoreCase))
                visibleEntries = visibleEntries.Where(e => string.Equals(e.SheetPrefix, selectedType, StringComparison.OrdinalIgnoreCase));

            List<SpriteSheetEntry> entries = visibleEntries.ToList();

            SourceCountText.Text = $"{entries.Count} visible sheet(s).";

            if (entries.Count == 0)
            {
                SourceButtonsPanel.Children.Add(new TextBlock
                {
                    Text = $"No sprite sheets found.\n\nPut item<number> or player<number> image files in:\n{SpriteArchiveDir}",
                    Foreground = Brushes.DimGray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(4)
                });
                return;
            }

            foreach (SpriteSheetEntry entry in entries)
            {
                Button button = new Button
                {
                    Content = $"{entry.FileName}\n{entry.SheetPrefix} IDs {entry.FirstSpriteId}-{entry.LastSpriteId}",
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 4, 4),
                    Padding = new Thickness(8, 5, 8, 5),
                    Tag = entry
                };

                button.Click += (_, _) => OpenSheet(entry);

                SourceButtonsPanel.Children.Add(button);
            }
        }

        private void OpenSheet(SpriteSheetEntry entry)
        {
            _currentEntry = entry;

            SpriteGridPanel.Children.Clear();

            SheetInfoText.Text =
                $"{entry.FileName} | Prefix: {entry.SheetPrefix} | IDs {entry.FirstSpriteId}-{entry.LastSpriteId}";

            for (int index = 0; index < entry.SpriteCount; index++)
            {
                int spriteId = entry.FirstSpriteId + index;

                BitmapSource? tile = GetTileImage(entry, spriteId);
                if (tile == null)
                    continue;

                Image image = new Image
                {
                    Source = tile,
                    Width = SpriteDisplaySize,
                    Height = SpriteDisplaySize,
                    Stretch = Stretch.Fill,
                    SnapsToDevicePixels = true
                };

                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

                Border imageFrame = new Border
                {
                    Width = SpriteDisplaySize,
                    Height = SpriteDisplaySize,
                    Background = Brushes.White,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Child = image
                };

                Button button = new Button
                {
                    Content = imageFrame,
                    Width = SpriteButtonSize,
                    Height = SpriteButtonSize,
                    Margin = new Thickness(1),
                    Padding = new Thickness(1),
                    Tag = spriteId,
                    ToolTip = $"{entry.FileName} | Sprite ID {spriteId}"
                };

                button.PreviewMouseMove += (_, mouseEvent) =>
                {
                    if (mouseEvent.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
                        return;

                    SelectedSprite draggedSprite = new SelectedSprite
                    {
                        SourceId = entry.SourceId,
                        SheetPrefix = entry.SheetPrefix,
                        SheetNumber = entry.SheetNumber,
                        SpriteId = spriteId,
                        Label = spriteId.ToString(),
                        SourceName = entry.FileName
                    };

                    DragDrop.DoDragDrop(button, draggedSprite, DragDropEffects.Copy);
                };
                button.MouseRightButtonDown += (_, eventArgs) =>
                {
                    AddSprite(entry, spriteId);
                    eventArgs.Handled = true;
                };


                SpriteGridPanel.Children.Add(button);
            }

            StatusText.Text = $"Opened {entry.FileName}. Click a sprite to add it to the output queue.";
        }


        private static BitmapSource? GetTileImage(SpriteSheetEntry entry, int spriteId)
        {
            int index = spriteId - entry.FirstSpriteId;

            if (index < 0 || index >= entry.SpriteCount)
                return null;

            int col = index % entry.Columns;
            int row = index / entry.Columns;

            Int32Rect rect = new Int32Rect(
                col * SpriteSize,
                row * SpriteSize,
                SpriteSize,
                SpriteSize
            );

            try
            {
                CroppedBitmap crop = new CroppedBitmap(entry.Image, rect);
                crop.Freeze();
                return crop;
            }
            catch
            {
                return null;
            }
        }
        private void AddSprite(SpriteSheetEntry entry, int spriteId)
        {
            OutputSpriteSlot? emptySlot = _outputSlots.FirstOrDefault(slot => slot.Sprite == null);

            if (emptySlot == null)
            {
                MessageBox.Show(
                    this,
                    $"The output sheet is full. A 10x10 sheet can only hold {MaxOutputSprites} sprites.",
                    "Output Full",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            emptySlot.Sprite = new SelectedSprite
            {
                SourceId = entry.SourceId,
                SheetPrefix = entry.SheetPrefix,
                SheetNumber = entry.SheetNumber,
                SpriteId = spriteId,
                Label = spriteId.ToString(),
                SourceName = entry.FileName
            };

            _selectedOutputSlot = emptySlot;

            RefreshOutputSlots();

            StatusText.Text = $"Added sprite {spriteId} to output slot {emptySlot.DisplaySlot}.";
        }
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedSlot(-OutputColumns);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedSlot(OutputColumns);
        }

        private void MoveSelectedSlot(int delta)
        {
            if (_selectedOutputSlot == null)
                return;

            if (_selectedOutputSlot.Sprite == null)
                return;

            int targetIndex = _selectedOutputSlot.SlotIndex + delta;

            if (targetIndex < 0 || targetIndex >= _outputSlots.Count)
                return;

            OutputSpriteSlot targetSlot = _outputSlots[targetIndex];

            (_selectedOutputSlot.Sprite, targetSlot.Sprite) = (targetSlot.Sprite, _selectedOutputSlot.Sprite);

            _selectedOutputSlot = targetSlot;

            RefreshOutputSlots();

            StatusText.Text = $"Moved sprite to output slot {targetSlot.DisplaySlot}.";
        }


        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOutputSlot == null)
                return;

            if (_selectedOutputSlot.Sprite == null)
                return;

            int removedId = _selectedOutputSlot.Sprite.SpriteId;
            _selectedOutputSlot.Sprite = null;

            RefreshOutputSlots();

            StatusText.Text = $"Cleared sprite {removedId} from output slot {_selectedOutputSlot.DisplaySlot}.";
        }

        private void ClearQueue_Click(object sender, RoutedEventArgs e)
        {
            int filledCount = _outputSlots.Count(slot => slot.Sprite != null);

            if (filledCount == 0)
                return;

            MessageBoxResult result = MessageBox.Show(
                this,
                $"Clear all {filledCount} placed sprite(s) from the output sheet?",
                "Clear Output Sheet",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
                return;

            foreach (OutputSpriteSlot slot in _outputSlots)
                slot.Sprite = null;

            RefreshOutputSlots();

            StatusText.Text = "Output sheet cleared.";
        }

        private void RefreshOutputSlots()
        {
            RefreshQueuePreview();

            if (_selectedOutputSlot != null)
                HighlightSelectedSlot(_selectedOutputSlot);

            
        }

        private void RefreshQueuePreview()
        {
            QueuePreviewPanel.Children.Clear();

            foreach (OutputSpriteSlot slot in _outputSlots)
            {
                Border slotBorder = new Border
                {
                    Width = SpriteButtonSize,
                    Height = SpriteButtonSize,
                    Background = Brushes.White,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(1),
                    AllowDrop = true,
                    Tag = slot
                };

                if (slot.Sprite != null)
                {
                    SpriteSheetEntry? entry = _entries.FirstOrDefault(e => e.SourceId == slot.Sprite.SourceId);

                    if (entry != null)
                    {
                        BitmapSource? tile = GetTileImage(entry, slot.Sprite.SpriteId);

                        if (tile != null)
                        {
                            Image image = new Image
                            {
                                Source = tile,
                                Width = SpriteDisplaySize,
                                Height = SpriteDisplaySize,
                                Stretch = Stretch.Fill,
                                SnapsToDevicePixels = true
                            };

                            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

                            slotBorder.Child = image;
                        }
                    }

                    slotBorder.ToolTip = $"Slot {slot.DisplaySlot} | Sprite ID {slot.Sprite.SpriteId} | Right-click to remove";
                }
                else
                {
                    slotBorder.ToolTip = $"Slot {slot.DisplaySlot} | Empty";
                }

                slotBorder.MouseLeftButtonDown += (_, _) =>
                {
                    _selectedOutputSlot = slot;
                    HighlightSelectedSlot(slot);
                };

                slotBorder.MouseRightButtonDown += (_, _) =>
                {
                    if (slot.Sprite == null)
                        return;

                    int removedId = slot.Sprite.SpriteId;
                    slot.Sprite = null;

                    _selectedOutputSlot = slot;
                    RefreshOutputSlots();

                    StatusText.Text = $"Cleared sprite {removedId} from output slot {slot.DisplaySlot}.";
                };

                slotBorder.Drop += (_, dropEvent) =>
                {
                    if (!dropEvent.Data.GetDataPresent(typeof(SelectedSprite)))
                        return;

                    SelectedSprite droppedSprite =
                        (SelectedSprite)dropEvent.Data.GetData(typeof(SelectedSprite))!;

                    slot.Sprite = droppedSprite;

                    _selectedOutputSlot = slot;
                    RefreshOutputSlots();

                    StatusText.Text = $"Placed sprite {droppedSprite.SpriteId} into output slot {slot.DisplaySlot}.";
                };
                slotBorder.DragOver += (_, dragEvent) =>
                {
                    if (dragEvent.Data.GetDataPresent(typeof(SelectedSprite)))
                    {
                        dragEvent.Effects = DragDropEffects.Copy;
                        dragEvent.Handled = true;
                    }
                };
                slotBorder.Drop += (_, dropEvent) =>
                {
                    if (!dropEvent.Data.GetDataPresent(typeof(SelectedSprite)))
                        return;

                    SelectedSprite droppedSprite =
                        (SelectedSprite)dropEvent.Data.GetData(typeof(SelectedSprite))!;

                    slot.Sprite = droppedSprite;

                    _selectedOutputSlot = slot;
                    RefreshOutputSlots();

                    StatusText.Text = $"Placed sprite {droppedSprite.SpriteId} into output slot {slot.DisplaySlot}.";
                };

                QueuePreviewPanel.Children.Add(slotBorder);
            }
        }

        private void HighlightSelectedSlot(OutputSpriteSlot selectedSlot)
        {
            foreach (object child in QueuePreviewPanel.Children)
            {
                if (child is not Border border)
                    continue;

                if (border.Tag is OutputSpriteSlot slot && slot == selectedSlot)
                {
                    border.BorderBrush = Brushes.DodgerBlue;
                    border.BorderThickness = new Thickness(3);
                }
                else
                {
                    border.BorderBrush = Brushes.Gray;
                    border.BorderThickness = new Thickness(1);
                }
            }
        }
private void GenerateSheet_Click(object sender, RoutedEventArgs e)
{
    int filledCount = _outputSlots.Count(slot => slot.Sprite != null);

    if (filledCount == 0)
    {
        MessageBox.Show(this, "Place at least one sprite first.", "Generate Sheet");
        return;
    }

    string safeName = SafeFileName(OutputNameTextBox.Text, "new_spritesheet");

    SaveFileDialog dialog = new SaveFileDialog
    {
        Title = "Save generated spritesheet",
        InitialDirectory = SpriteDir,
        FileName = safeName + ".bmp",
        DefaultExt = ".bmp",
        Filter = "Bitmap (*.bmp)|*.bmp|PNG (*.png)|*.png|All files (*.*)|*.*"
    };

    if (dialog.ShowDialog(this) != true)
        return;

    BitmapSource output = BuildOutputSheet();

    BitmapEncoder encoder;

    string extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();

    if (extension == ".png")
        encoder = new PngBitmapEncoder();
    else
        encoder = new BmpBitmapEncoder();

    encoder.Frames.Add(BitmapFrame.Create(output));

    using FileStream stream = File.Create(dialog.FileName);
    encoder.Save(stream);

    int blankSlots = MaxOutputSprites - filledCount;

    StatusText.Text =
        $"Generated {Path.GetFileName(dialog.FileName)} as a 10x10 sheet with {filledCount} sprite(s) and {blankSlots} blank slot(s).";

    MessageBox.Show(
        this,
        $"Saved 10x10 sheet:\n{dialog.FileName}\n\nFilled sprites: {filledCount}\nBlank slots: {blankSlots}",
        "Generated",
        MessageBoxButton.OK,
        MessageBoxImage.Information
    );
}
        private BitmapSource BuildOutputSheet()
        {
            int width = OutputColumns * SpriteSize;
            int height = OutputRows * SpriteSize;

            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                foreach (OutputSpriteSlot slot in _outputSlots)
                {
                    if (slot.Sprite == null)
                        continue;

                    SpriteSheetEntry? entry = _entries.FirstOrDefault(e => e.SourceId == slot.Sprite.SourceId);

                    if (entry == null)
                        continue;

                    BitmapSource? tile = GetTileImage(entry, slot.Sprite.SpriteId);

                    if (tile == null)
                        continue;

                    int x = (slot.SlotIndex % OutputColumns) * SpriteSize;
                    int y = (slot.SlotIndex / OutputColumns) * SpriteSize;

                    dc.DrawImage(tile, new Rect(x, y, SpriteSize, SpriteSize));
                }
            }

            RenderTargetBitmap render = new RenderTargetBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Pbgra32
            );

            render.Render(visual);
            render.Freeze();

            return render;
        }
        private static string SafeFileName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            string cleaned = Regex.Replace(value.Trim(), @"[^A-Za-z0-9_.-]+", "_");
            cleaned = cleaned.Trim('.', '_', '-');

            return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
        }
    }

    public sealed class SpriteSheetEntry
    {
        public required string Path { get; init; }
        public required string SheetPrefix { get; init; }
        public required int SheetNumber { get; init; }
        public required int Columns { get; init; }
        public required int Rows { get; init; }
        public required BitmapSource Image { get; init; }

        public string SourceId => System.IO.Path.GetFullPath(Path);
        public string FileName => System.IO.Path.GetFileName(Path);

        public int FirstSpriteId => SheetNumber * 100;
        public int SpriteCount => Columns * Rows;
        public int LastSpriteId => FirstSpriteId + SpriteCount - 1;
    }

    public sealed class SelectedSprite
    {
        public int DisplayOrder { get; set; }

        public required string SourceId { get; init; }
        public required string SheetPrefix { get; init; }
        public required int SheetNumber { get; init; }
        public required int SpriteId { get; init; }
        public required string Label { get; init; }
        public required string SourceName { get; init; }

        public string DisplaySprite => $"{SheetPrefix} ID {SpriteId}";
    }

    public sealed class OutputSpriteSlot
    {
        public int SlotIndex { get; set; }

        public SelectedSprite? Sprite { get; set; }

        public int DisplaySlot => SlotIndex + 1;

        public string DisplaySprite => Sprite == null
            ? ""
            : Sprite.DisplaySprite;

        public string SourceName => Sprite == null
            ? ""
            : Sprite.SourceName;
    }
}