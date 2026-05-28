using Microsoft.Win32;
using RPGWO.Formats.Ini;
using RPGWO.ServerTool.Models;
using RPGWO.ServerTool.Services;
using RPGWO_INI_Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinForms = System.Windows.Forms;

namespace RPGWO.ServerTool
{
    public partial class MainWindow : Window
    {

        #region Services

        private readonly ServerWorkspaceService _workspaceService = new();
        private readonly WorldEditorService _worldEditorService = new();
        private readonly CsvExportService _csvExportService = new();
        private readonly SkillEditorService _skillEditorService = new();
        private readonly AnimationEditorService _animationEditorService = new();
        private readonly TreasureEditorService _treasureEditorService = new();
        private ObservableCollection<TreasureEntryRow> _treasureEntryRows = new();
        private ObservableCollection<TreasureFieldRow> _selectedTreasureEntryFieldRows = new();
        private int? _selectedTreasureEntryIndex;
        private readonly MagicEditorService _magicEditorService = new();
        private readonly MonsterEditorService _monsterEditorService = new();
        private readonly ItemEditorService _itemEditorService = new();
        private readonly UsageEditorService _usageEditorService = new();
        private readonly MultiUseEditorService _multiUseEditorService = new();
        private readonly IniEntryPreviewService _previewService = new();
        private readonly ItemSpriteService _itemSpriteService = new();

        private readonly SoundLibraryService _soundLibraryService = new();

        private readonly MediaPlayer _soundPreviewPlayer = new();

        #endregion


        private ServerWorkspace? _workspace;
        private string? _serverFolderPath;
        private string? _worldIniPath;
        private string? _skillIniPath;
        private string? _itemIniPath;

        private ObservableCollection<ItemRow> _itemRows = new();
        private ObservableCollection<ItemRow> _filteredItemRows = new();
        private ObservableCollection<ItemFieldRow> _itemFieldRows = new();
        private int? _selectedItemId;

        private bool _isInitialized;

        private string? _animationIniPath;


        public MainWindow()
        {
            InitializeComponent();

            _isInitialized = true;
            DataContext = this;

            SelectNavigation("Dashboard");
            ShowDashboardView();
        }




        #region Animation
        private ObservableCollection<AnimationRow> _animationRows = new();
        private ObservableCollection<AnimationRow> _filteredAnimationRows = new();
        private ObservableCollection<AnimationFieldRow> _animationFieldRows = new();
        private int? _selectedAnimationId;

        private void AnimationSoundPickButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is AnimationFieldRow row &&
                row.Type.Equals("Field", StringComparison.OrdinalIgnoreCase) &&
                row.Key.Equals("Sound", StringComparison.OrdinalIgnoreCase))
            {
                button.Visibility = Visibility.Visible;
                button.IsEnabled = true;
            }
            else
            {
                button.Visibility = Visibility.Collapsed;
                button.IsEnabled = false;
            }
        }
        private void PickAnimationSoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.Tag is not AnimationFieldRow targetRow)
            {
                StatusText.Text = "Could not determine which animation sound row to edit.";
                return;
            }

            if (!targetRow.Key.Equals("Sound", StringComparison.OrdinalIgnoreCase))
            {
                StatusText.Text = "Only Sound rows can use the sound picker.";
                return;
            }

            OpenAnimationSoundPicker(targetRow);
        }
        private void OpenAnimationSoundPicker(AnimationFieldRow targetRow)
        {
            if (string.IsNullOrWhiteSpace(_serverFolderPath))
            {
                StatusText.Text = "Open a server folder before picking sounds.";
                return;
            }

            _soundLibraryService.EnsureSoundFolder(_serverFolderPath);

            List<SoundRow> sounds = _soundLibraryService.LoadSounds(_serverFolderPath);

            var picker = new Window
            {
                Title = "Pick animation sound",
                Owner = this,
                Width = 760,
                Height = 560,
                MinWidth = 560,
                MinHeight = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel
            {
                Margin = new Thickness(10)
            };

            var topPanel = new DockPanel
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            var closeButton = new Button
            {
                Content = "Close",
                Width = 90,
                Height = 28
            };

            DockPanel.SetDock(closeButton, Dock.Right);
            topPanel.Children.Add(closeButton);

            var importButton = new Button
            {
                Content = "Import .wav...",
                Width = 110,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 0)
            };

            DockPanel.SetDock(importButton, Dock.Right);
            topPanel.Children.Add(importButton);

            var searchBox = new TextBox
            {
                MinWidth = 220,
                VerticalAlignment = VerticalAlignment.Center
            };

            topPanel.Children.Add(searchBox);

            DockPanel.SetDock(topPanel, Dock.Top);
            root.Children.Add(topPanel);

            var bottomPanel = new DockPanel
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            var statusText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.75
            };

            DockPanel.SetDock(statusText, Dock.Left);
            bottomPanel.Children.Add(statusText);

            var stopButton = new Button
            {
                Content = "Stop",
                Width = 80,
                Height = 28,
                Margin = new Thickness(8, 0, 0, 0)
            };

            DockPanel.SetDock(stopButton, Dock.Right);
            bottomPanel.Children.Add(stopButton);

            var playButton = new Button
            {
                Content = "Play",
                Width = 80,
                Height = 28
            };

            DockPanel.SetDock(playButton, Dock.Right);
            bottomPanel.Children.Add(playButton);

            DockPanel.SetDock(bottomPanel, Dock.Bottom);
            root.Children.Add(bottomPanel);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "File",
                Binding = new System.Windows.Data.Binding("FileName"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Size",
                Binding = new System.Windows.Data.Binding("SizeText"),
                Width = new DataGridLength(100)
            });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<SoundRow> filtered = sounds;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(sound =>
                        sound.FileName.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<SoundRow> visible = filtered.ToList();

                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {sounds.Count} sound(s). Folder: Sounds";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not SoundRow sound)
                    return;

                targetRow.Value = sound.FileName;
                targetRow.Enabled = true;

                AnimationFieldGrid.Items.Refresh();

                picker.Close();

                StatusText.Text = $"Set animation Sound to {sound.FileName}.";
            }

            void PlayCurrent()
            {
                if (grid.SelectedItem is not SoundRow sound)
                    return;

                try
                {
                    _soundPreviewPlayer.Stop();
                    _soundPreviewPlayer.Open(new Uri(sound.FullPath, UriKind.Absolute));
                    _soundPreviewPlayer.Play();

                    StatusText.Text = $"Playing {sound.FileName}.";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Could not play sound: {ex.Message}";
                    MessageBox.Show(ex.Message, "Sound playback failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            void StopCurrent()
            {
                _soundPreviewPlayer.Stop();
            }

            void ImportSounds()
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Import WAV sound files",
                    Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*",
                    Multiselect = true
                };

                if (dialog.ShowDialog(this) != true)
                    return;

                string soundFolder = _soundLibraryService.GetSoundFolder(_serverFolderPath);

                foreach (string sourcePath in dialog.FileNames)
                {
                    string fileName = Path.GetFileName(sourcePath);
                    string destinationPath = Path.Combine(soundFolder, fileName);

                    File.Copy(sourcePath, destinationPath, overwrite: true);
                }

                sounds = _soundLibraryService.LoadSounds(_serverFolderPath);
                Refresh();

                StatusText.Text = $"Imported {dialog.FileNames.Length} sound file(s).";
            }

            searchBox.TextChanged += (_, _) => Refresh();

            grid.MouseDoubleClick += (_, _) => SelectCurrent();

            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            playButton.Click += (_, _) => PlayCurrent();
            stopButton.Click += (_, _) => StopCurrent();
            importButton.Click += (_, _) => ImportSounds();
            closeButton.Click += (_, _) => picker.Close();

            picker.Closed += (_, _) => _soundPreviewPlayer.Stop();

            Refresh();
            picker.ShowDialog();
        }


        private void SaveAnimation()
        {
            if (string.IsNullOrWhiteSpace(_animationIniPath) ||
                !File.Exists(_animationIniPath))
            {
                StatusText.Text = "animation.ini was not found.";
                return;
            }

            if (_selectedAnimationId is null)
            {
                StatusText.Text = "Select an animation before saving.";
                return;
            }

            try
            {
                AnimationFieldGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                AnimationFieldGrid.CommitEdit(DataGridEditingUnit.Row, true);

                SaveValidationResult result = _animationEditorService.SaveAnimationRows(
                    _animationIniPath,
                    _selectedAnimationId.Value,
                    _animationFieldRows.ToList());

                if (!result.Success)
                {
                    StatusText.Text = $"Save failed: {result.ErrorMessage}";

                    System.Windows.MessageBox.Show(
                        result.ErrorMessage ?? "Save failed.",
                        "Save failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                StatusText.Text =
                    $"{result.Message} Backup: {result.BackupPath}";

                System.Windows.MessageBox.Show(
                    $"{result.Message}\n\nBackup created:\n{result.BackupPath}",
                    "Animation saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                int savedAnimationId = _selectedAnimationId.Value;

                ReloadWorkspaceAfterSave();

                AnimationListGrid.ItemsSource = null;
                AnimationFieldGrid.ItemsSource = null;

                LoadAnimationRowsIfNeeded();

                AnimationRow? savedRow = _filteredAnimationRows.FirstOrDefault(row => row.Id == savedAnimationId);

                if (savedRow is not null)
                {
                    AnimationListGrid.SelectedItem = savedRow;
                    LoadSelectedAnimationFields(savedAnimationId);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Save animation.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void ShowAnimationView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Visible;
            TreasureView.Visibility = Visibility.Collapsed;
            MagicView.Visibility = Visibility.Collapsed;
            ItemView.Visibility = Visibility.Collapsed;
            MonsterView.Visibility = Visibility.Collapsed;
            UsageView.Visibility = Visibility.Collapsed;
            MultiUseView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;

            ExportCsvButton.IsEnabled = _animationIniPath is not null && File.Exists(_animationIniPath);

            LoadAnimationRowsIfNeeded();

            SaveButton.IsEnabled = AnimationFieldGrid.ItemsSource is not null;

            StatusText.Text = "Animation editor loaded.";
        }

        private void LoadAnimationRowsIfNeeded()
        {
            if (AnimationListGrid.ItemsSource is not null)
                return;

            if (string.IsNullOrWhiteSpace(_animationIniPath) ||
                !File.Exists(_animationIniPath))
            {
                StatusText.Text = "animation.ini was not found.";
                return;
            }

            try
            {
                List<AnimationRow> rows = _animationEditorService.LoadAnimationRows(_animationIniPath);

                _animationRows = new ObservableCollection<AnimationRow>(rows);
                _filteredAnimationRows = new ObservableCollection<AnimationRow>(rows);

                AnimationListGrid.ItemsSource = _filteredAnimationRows;
                AnimationFieldGrid.ItemsSource = null;
                _selectedAnimationId = null;
                SelectedAnimationText.Text = "No animation selected.";
                AnimationPreviewStrip.Children.Clear();
                AnimationPreviewText.Text = "Frames: -";

                StatusText.Text = $"Loaded animation.ini editor with {_animationRows.Count} animation(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load animation.ini: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load animation.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AnimationSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AnimationListGrid is null)
                return;

            RefreshAnimationFilter();
        }

        private void AnimationListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnimationListGrid.SelectedItem is not AnimationRow selectedAnimation)
                return;

            LoadSelectedAnimationFields(selectedAnimation.Id);
        }

        private void LoadSelectedAnimationFields(int animationId)
        {
            if (string.IsNullOrWhiteSpace(_animationIniPath) ||
                !File.Exists(_animationIniPath))
            {
                StatusText.Text = "animation.ini was not found.";
                return;
            }

            try
            {
                List<AnimationFieldRow> rows = _animationEditorService.LoadFieldRows(
                    _animationIniPath,
                    animationId);

                _animationFieldRows = new ObservableCollection<AnimationFieldRow>(rows);
                AnimationFieldGrid.ItemsSource = _animationFieldRows;

                UpdateAnimationPreview(); 

                AnimationRawPreviewText.Text = _previewService.GetAnimationPreview(
                                                                _animationIniPath,
                                                                animationId);

                AnimationRow? animation = _animationRows.FirstOrDefault(row => row.Id == animationId);

                SelectedAnimationText.Text = animation is null
                    ? $"Animation {animationId}"
                    : $"Animation {animation.Id}: {animation.Name}";

                SaveButton.IsEnabled = true;

                StatusText.Text = $"Loaded Animation={animationId} with {_animationFieldRows.Count} editable row(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load Animation={animationId}: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load animation failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RefreshAnimationFilter()
        {
            string needle = AnimationSearchBox.Text.Trim();

            IEnumerable<AnimationRow> filtered = _animationRows;

            if (!string.IsNullOrWhiteSpace(needle))
            {
                filtered = filtered.Where(animation =>
                    animation.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    animation.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            _filteredAnimationRows = new ObservableCollection<AnimationRow>(filtered);

            AnimationListGrid.ItemsSource = _filteredAnimationRows;
        }
        private void CommitAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            AnimationFieldGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            AnimationFieldGrid.CommitEdit(DataGridEditingUnit.Row, true);

            UpdateAnimationPreview();

            SaveAnimation();
        }
        private void AnimationFieldGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(UpdateAnimationPreview));
        }
        private void AnimationFramePickButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is AnimationFieldRow row &&
                row.Type.Equals("Field", StringComparison.OrdinalIgnoreCase) &&
                row.Key.Equals("Frame", StringComparison.OrdinalIgnoreCase))
            {
                button.Visibility = Visibility.Visible;
                button.IsEnabled = true;
            }
            else
            {
                button.Visibility = Visibility.Collapsed;
                button.IsEnabled = false;
            }
        }
        private void PickAnimationFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.Tag is not AnimationFieldRow targetRow)
            {
                StatusText.Text = "Could not determine which animation frame row to edit.";
                return;
            }

            if (!targetRow.Key.Equals("Frame", StringComparison.OrdinalIgnoreCase))
            {
                StatusText.Text = "Only Frame rows can use the sprite picker.";
                return;
            }

            OpenAnimationFrameSpritePicker(targetRow);
        }
        private void OpenAnimationFrameSpritePicker(AnimationFieldRow targetRow)
        {
            if (string.IsNullOrWhiteSpace(_serverFolderPath))
            {
                StatusText.Text = "Open a server folder before picking sprites.";
                return;
            }

            string spritesFolder = Path.Combine(_serverFolderPath, "Sprites");

            if (!Directory.Exists(spritesFolder))
            {
                StatusText.Text = $"Sprites folder not found: {spritesFolder}";

                MessageBox.Show(
                    $"Could not find the Sprites folder:\n\n{spritesFolder}",
                    "Sprites folder missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            List<ItemSpriteTile> tiles;

            try
            {
                tiles = LoadItemSpriteTiles(spritesFolder);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load sprites: {ex.Message}";

                MessageBox.Show(
                    ex.Message,
                    "Sprite load failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (tiles.Count == 0)
            {
                StatusText.Text = "No item spritesheets were found.";

                MessageBox.Show(
                    $"No usable item sprite sheets were found in:\n\n{spritesFolder}\n\nExpected names like item0.bmp, item1.bmp, item2.png, etc.",
                    "No item sprites found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var picker = new Window
            {
                Title = "Pick animation frame sprite",
                Owner = this,
                Width = 1200,
                Height = 800,
                MinWidth = 900,
                MinHeight = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel
            {
                Margin = new Thickness(10)
            };

            var topPanel = new Grid
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchLabel = new TextBlock
            {
                Text = "Search ID:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var searchBox = new TextBox
            {
                MinWidth = 140,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var sheetLabel = new TextBlock
            {
                Text = "Sheet:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var sheetFilter = new ComboBox
            {
                Margin = new Thickness(0, 0, 12, 0)
            };

            var closeButton = new Button
            {
                Content = "Close",
                MinWidth = 90
            };

            Grid.SetColumn(searchLabel, 0);
            Grid.SetColumn(searchBox, 1);
            Grid.SetColumn(sheetLabel, 2);
            Grid.SetColumn(sheetFilter, 3);
            Grid.SetColumn(closeButton, 4);

            topPanel.Children.Add(searchLabel);
            topPanel.Children.Add(searchBox);
            topPanel.Children.Add(sheetLabel);
            topPanel.Children.Add(sheetFilter);
            topPanel.Children.Add(closeButton);

            DockPanel.SetDock(topPanel, Dock.Top);
            root.Children.Add(topPanel);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var wrap = new WrapPanel
            {
                Orientation = Orientation.Horizontal
            };

            scroll.Content = wrap;
            root.Children.Add(scroll);

            picker.Content = root;

            sheetFilter.Items.Add("All");

            foreach (string sheetName in tiles.Select(tile => tile.SheetName).Distinct().OrderBy(value => value))
                sheetFilter.Items.Add(sheetName);

            sheetFilter.SelectedItem = "All";

            void RenderTiles()
            {
                wrap.Children.Clear();

                string search = searchBox.Text.Trim();
                string selectedSheet = sheetFilter.SelectedItem?.ToString() ?? "All";

                IEnumerable<ItemSpriteTile> filtered = tiles;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(tile =>
                        tile.SpriteId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.Equals(selectedSheet, "All", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(tile =>
                        string.Equals(tile.SheetName, selectedSheet, StringComparison.OrdinalIgnoreCase));
                }

                List<ItemSpriteTile> visibleTiles = filtered.ToList();

                foreach (ItemSpriteTile tile in visibleTiles)
                {
                    var image = new Image
                    {
                        Source = tile.Image,
                        Width = 32,
                        Height = 32,
                        Stretch = Stretch.None,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var idText = new TextBlock
                    {
                        Text = tile.SpriteId.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 11
                    };

                    var stack = new StackPanel
                    {
                        Orientation = Orientation.Vertical
                    };

                    stack.Children.Add(image);
                    stack.Children.Add(idText);

                    var button = new Button
                    {
                        Content = stack,
                        Width = 62,
                        Height = 68,
                        Margin = new Thickness(3),
                        ToolTip = $"{tile.SheetName} | ID {tile.SpriteId}"
                    };

                    button.Click += (_, _) =>
                    {
                        targetRow.Value = tile.SpriteId.ToString();
                        targetRow.Enabled = true;

                        AnimationFieldGrid.Items.Refresh();
                        UpdateAnimationPreview();

                        picker.Close();

                        StatusText.Text = $"Set animation Frame to sprite ID {tile.SpriteId}.";
                    };

                    wrap.Children.Add(button);
                }

                statusText.Text = $"Showing {visibleTiles.Count} of {tiles.Count} sprite(s).";
            }

            searchBox.TextChanged += (_, _) => RenderTiles();
            sheetFilter.SelectionChanged += (_, _) => RenderTiles();
            closeButton.Click += (_, _) => picker.Close();

            RenderTiles();
            picker.ShowDialog();
        }

        private void UpdateAnimationPreview()
        {
            if (AnimationPreviewStrip is null || AnimationPreviewText is null)
                return;

            AnimationPreviewStrip.Children.Clear();

            if (string.IsNullOrWhiteSpace(_serverFolderPath))
            {
                AnimationPreviewText.Text = "Frames: -";
                return;
            }

            List<AnimationFieldRow> frameRows = _animationFieldRows
                .Where(row =>
                    row.Enabled &&
                    row.Type.Equals("Field", StringComparison.OrdinalIgnoreCase) &&
                    row.Key.Equals("Frame", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(row.Value?.Trim(), out _))
                .ToList();

            if (frameRows.Count == 0)
            {
                AnimationPreviewText.Text = "Frames: -";
                return;
            }

            var frameIds = new List<string>();

            foreach (AnimationFieldRow row in frameRows)
            {
                if (!int.TryParse(row.Value?.Trim(), out int spriteId))
                    continue;

                var frameBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4),
                    Margin = new Thickness(0, 0, 8, 0),
                    ToolTip = $"Frame={spriteId}"
                };

                frameBorder.Child = CreateAnimationPreviewSpriteImage(spriteId);

                AnimationPreviewStrip.Children.Add(frameBorder);
                frameIds.Add(spriteId.ToString());
            }

            AnimationPreviewText.Text = frameIds.Count == 0
                ? "Frames: -"
                : $"Frames: {string.Join(", ", frameIds)}";
        }
        private Image CreateAnimationPreviewSpriteImage(int spriteId)
        {
            ImageSource? sprite = _itemSpriteService.GetItemSpriteImage(_serverFolderPath, spriteId.ToString());

            return new Image
            {
                Source = sprite,
                Width = 32,
                Height = 32,
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        private void ShowPlaceholderView(string editorName)
        {
            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Visible;

            PlaceholderText.Text = $"{editorName} editor is not implemented yet.";

            SaveButton.IsEnabled = false;
            ExportCsvButton.IsEnabled = false;

            StatusText.Text = $"{editorName} editor is planned.";
        }




        #endregion

        #region Skill
        private ObservableCollection<SkillRow> _skillRows = new();
        private ObservableCollection<SkillRow> _filteredSkillRows = new();
        private ObservableCollection<SkillFieldRow> _skillFieldRows = new();
        private int? _selectedSkillId;

        private void ShowSkillView()
        {
            EnsureSkillFieldGridsConfigured();

            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Visible;
            AnimationView.Visibility = Visibility.Collapsed;
            TreasureView.Visibility = Visibility.Collapsed;
            MagicView.Visibility = Visibility.Collapsed;
            MonsterView.Visibility = Visibility.Collapsed;
            ItemView.Visibility = Visibility.Collapsed;
            UsageView.Visibility = Visibility.Collapsed;
            MultiUseView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;

            ExportCsvButton.IsEnabled = _skillIniPath is not null && File.Exists(_skillIniPath);

            LoadSkillRowsIfNeeded();

            SaveButton.IsEnabled = SkillGeneralFieldGrid.ItemsSource is not null;

            StatusText.Text = "Skill editor loaded.";
        }

        private void CommitSkillButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSkill();
        }

        private void NewSkillButton_Click(object sender, RoutedEventArgs e)
        {
            int nextId = _skillRows.Any() ? _skillRows.Max(row => row.Id) + 1 : 1;

            LoadSkillFieldRowsIntoTabs(_skillEditorService.CreateBlankFieldRows(nextId));
            _selectedSkillId = null;
            SelectedSkillText.Text = $"New Skill: {nextId}";
            SkillRawPreviewText.Text = "";
            SaveButton.IsEnabled = true;
            StatusText.Text = $"Started new Skill={nextId}.";
        }

        private void DeleteSkillButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_skillIniPath) || !File.Exists(_skillIniPath))
            {
                StatusText.Text = "skill.ini was not found.";
                return;
            }

            if (SkillListGrid.SelectedItem is not SkillRow selectedSkill)
            {
                StatusText.Text = "Select a skill before deleting.";
                return;
            }

            MessageBoxResult confirm = System.Windows.MessageBox.Show(
                $"Delete Skill={selectedSkill.Id} ({selectedSkill.Name})?",
                "Delete skill",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                _skillEditorService.DeleteSkill(_skillIniPath, selectedSkill.Id);
                ReloadWorkspaceAfterSave();
                SkillListGrid.ItemsSource = null;
                ClearSkillEditorOnly();
                LoadSkillRowsIfNeeded();
                StatusText.Text = $"Deleted Skill={selectedSkill.Id}.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Delete failed: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Delete skill failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSkill()
        {
            if (string.IsNullOrWhiteSpace(_skillIniPath) || !File.Exists(_skillIniPath))
            {
                StatusText.Text = "skill.ini was not found.";
                return;
            }

            try
            {
                CommitSkillGrids();
                _skillEditorService.SaveSkillRows(_skillIniPath, _skillFieldRows.ToList());

                int savedId = int.Parse(_skillFieldRows.First(row => row.Label == "Skill").Value);

                ReloadWorkspaceAfterSave();
                SkillListGrid.ItemsSource = null;
                LoadSkillRowsIfNeeded();
                RefreshSkillFilter();

                SkillRow? savedRow = _filteredSkillRows.FirstOrDefault(row => row.Id == savedId);
                if (savedRow is not null)
                {
                    SkillListGrid.SelectedItem = savedRow;
                    LoadSelectedSkillFields(savedId);
                }

                StatusText.Text = $"Saved Skill={savedId}.";
                System.Windows.MessageBox.Show($"Saved Skill={savedId}.", "Skill saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Save skill.ini failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSkillRowsIfNeeded()
        {
            if (SkillListGrid.ItemsSource is not null)
                return;

            if (string.IsNullOrWhiteSpace(_skillIniPath) || !File.Exists(_skillIniPath))
            {
                StatusText.Text = "skill.ini was not found.";
                return;
            }

            try
            {
                List<SkillRow> rows = _skillEditorService.LoadSkillRows(_skillIniPath);

                _skillRows = new ObservableCollection<SkillRow>(rows);
                _filteredSkillRows = new ObservableCollection<SkillRow>(rows);

                SkillListGrid.ItemsSource = _filteredSkillRows;
                ClearSkillEditorOnly();

                StatusText.Text = $"Loaded skill.ini editor with {_skillRows.Count} skill(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load skill.ini: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Load skill.ini failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshSkillFilter()
        {
            string needle = SkillSearchBox.Text.Trim();

            IEnumerable<SkillRow> filtered = _skillRows;

            if (!string.IsNullOrWhiteSpace(needle))
            {
                filtered = filtered.Where(skill =>
                    skill.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    skill.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    skill.Purpose.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            _filteredSkillRows = new ObservableCollection<SkillRow>(filtered);
            SkillListGrid.ItemsSource = _filteredSkillRows;
        }

        private void SkillSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SkillListGrid is null)
                return;

            RefreshSkillFilter();
        }

        private void SkillListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SkillListGrid.SelectedItem is not SkillRow selectedSkill)
                return;

            LoadSelectedSkillFields(selectedSkill.Id);
        }

        private void LoadSelectedSkillFields(int skillId)
        {
            if (string.IsNullOrWhiteSpace(_skillIniPath) || !File.Exists(_skillIniPath))
            {
                StatusText.Text = "skill.ini was not found.";
                return;
            }

            try
            {
                List<SkillFieldRow> rows = _skillEditorService.LoadFieldRows(_skillIniPath, skillId);

                _selectedSkillId = skillId;
                LoadSkillFieldRowsIntoTabs(rows);

                SkillRawPreviewText.Text = _skillEditorService.GetSkillPreview(_skillIniPath, skillId);

                SkillRow? skill = _skillRows.FirstOrDefault(row => row.Id == skillId);

                SelectedSkillText.Text = skill is null
                    ? $"Skill {skillId}"
                    : $"Skill {skill.Id}: {skill.Name}";

                SaveButton.IsEnabled = true;

                StatusText.Text = $"Loaded Skill={skillId}.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load Skill={skillId}: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Load skill failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSkillFieldRowsIntoTabs(List<SkillFieldRow> rows)
        {
            _skillFieldRows = new ObservableCollection<SkillFieldRow>(rows);

            SkillGeneralFieldGrid.ItemsSource = _skillFieldRows
                .Where(row => row.Group == "Base" || row.Group == "General")
                .ToList();

            SkillAttributesFieldGrid.ItemsSource = _skillFieldRows
                .Where(row => row.Group == "Attributes")
                .ToList();

            SkillFlagsFieldGrid.ItemsSource = _skillFieldRows
                .Where(row => row.Group == "Flags")
                .ToList();
        }

        private void EnsureSkillFieldGridsConfigured()
        {
            DataGrid[] grids =
            {
                SkillGeneralFieldGrid,
                SkillAttributesFieldGrid,
                SkillFlagsFieldGrid
            };

            foreach (DataGrid grid in grids)
                ConfigureSkillFieldGrid(grid);
        }

        private void ConfigureSkillFieldGrid(DataGrid grid)
        {
            if (grid.Columns.Count > 0)
                return;

            grid.AutoGenerateColumns = false;
            grid.CanUserAddRows = false;
            grid.CanUserDeleteRows = false;
            grid.HeadersVisibility = DataGridHeadersVisibility.Column;
            grid.SelectionMode = DataGridSelectionMode.Single;

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Field",
                Binding = new System.Windows.Data.Binding("Label"),
                IsReadOnly = true,
                Width = new DataGridLength(180)
            });

            FrameworkElementFactory CreateValueComboFactory()
            {
                var factory = new FrameworkElementFactory(typeof(ComboBox));

                factory.SetBinding(ComboBox.TextProperty, new System.Windows.Data.Binding("Value")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                });

                factory.SetBinding(ItemsControl.ItemsSourceProperty, new System.Windows.Data.Binding("Options"));
                factory.SetValue(ComboBox.IsEditableProperty, true);

                return factory;
            }
            grid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Enabled / Flag",
                Binding = new System.Windows.Data.Binding("Enabled")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                },
                Width = new DataGridLength(110)
            });

            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Value",
                CellTemplate = new DataTemplate { VisualTree = CreateValueComboFactory() },
                CellEditingTemplate = new DataTemplate { VisualTree = CreateValueComboFactory() },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "INI Key",
                Binding = new System.Windows.Data.Binding("IniKey"),
                IsReadOnly = true,
                Width = new DataGridLength(140)
            });
        }

        private void CommitSkillGrids()
        {
            DataGrid[] grids =
            {
                SkillGeneralFieldGrid,
                SkillAttributesFieldGrid,
                SkillFlagsFieldGrid
            };

            Keyboard.ClearFocus();

            foreach (DataGrid grid in grids)
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }

        private void ClearSkillEditorOnly()
        {
            _selectedSkillId = null;
            _skillFieldRows = new ObservableCollection<SkillFieldRow>();
            LoadSkillFieldRowsIntoTabs(new List<SkillFieldRow>());
            SelectedSkillText.Text = "No skill selected.";
            SkillRawPreviewText.Text = "";
        }
        #endregion


        #region World

        private ObservableCollection<WorldSettingRow> _worldRows = new();

        private static readonly HashSet<string> WorldItemReferenceFields = new(StringComparer.OrdinalIgnoreCase)
{
    "MeteoriteItem"
};

        private static readonly HashSet<string> WorldMonsterReferenceFields = new(StringComparer.OrdinalIgnoreCase)
{
    "AutoStalkerID"
};

        private static readonly HashSet<string> WorldSingleSpriteReferenceFields = new(StringComparer.OrdinalIgnoreCase)
{
    "PassedOutImage",
    "CTFRedImage",
    "CTFBlueImage"
};

        private static readonly HashSet<string> WorldMultiSpriteReferenceFields = new(StringComparer.OrdinalIgnoreCase)
{
    "CTFRedResurrect",
    "CTFBlueResurrect",
    "CTFRedRessurect",
    "CTFBlueRessurect"
};

        private static bool IsWorldItemReferenceField(string key)
        {
            return WorldItemReferenceFields.Contains(key);
        }

        private static bool IsWorldMonsterReferenceField(string key)
        {
            return WorldMonsterReferenceFields.Contains(key);
        }

        private static bool IsWorldSingleSpriteReferenceField(string key)
        {
            return WorldSingleSpriteReferenceFields.Contains(key);
        }

        private static bool IsWorldMultiSpriteReferenceField(string key)
        {
            return WorldMultiSpriteReferenceFields.Contains(key);
        }

        private static bool IsWorldReferencePickerField(string key)
        {
            return IsWorldItemReferenceField(key)
                || IsWorldMonsterReferenceField(key)
                || IsWorldSingleSpriteReferenceField(key)
                || IsWorldMultiSpriteReferenceField(key);
        }

        private void ShowWorldView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Visible;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Collapsed;
            TreasureView.Visibility = Visibility.Collapsed;
            MagicView.Visibility = Visibility.Collapsed;
            ItemView.Visibility = Visibility.Collapsed;
            MonsterView.Visibility = Visibility.Collapsed;
            UsageView.Visibility = Visibility.Collapsed;
            MultiUseView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;

            ExportCsvButton.IsEnabled = _worldIniPath is not null && File.Exists(_worldIniPath);

            EnsureWorldGridsConfigured();
            LoadWorldRowsIfNeeded();

            SaveButton.IsEnabled = WorldGeneralGrid.ItemsSource is not null;

            StatusText.Text = "World editor loaded.";
        }

        private void CommitWorldButton_Click(object sender, RoutedEventArgs e)
        {
            SaveWorld();
        }

        private void EnsureWorldGridsConfigured()
        {
            ConfigureWorldGrid(WorldGeneralGrid);
            ConfigureWorldGrid(WorldMapGrid);
            ConfigureWorldGrid(WorldPlayerGrid);
            ConfigureWorldGrid(WorldMobGrid);
            ConfigureWorldGrid(WorldItemGrid);
            ConfigureWorldGrid(WorldGuildGrid);
            ConfigureWorldGrid(WorldCtfGrid);
            ConfigureWorldGrid(WorldMiscGrid);
        }

        private void ConfigureWorldGrid(DataGrid grid)
        {
            if (grid.Columns.Count > 0)
                return;

            grid.AutoGenerateColumns = false;
            grid.CanUserAddRows = false;
            grid.CanUserDeleteRows = false;
            grid.HeadersVisibility = DataGridHeadersVisibility.Column;
            grid.SelectionMode = DataGridSelectionMode.Single;

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Key / Flag",
                Binding = new System.Windows.Data.Binding("Key"),
                IsReadOnly = true,
                Width = new DataGridLength(220)
            });

            grid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Enabled / Flag",
                Binding = new System.Windows.Data.Binding("Enabled")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                },
                Width = new DataGridLength(110)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Value",
                Binding = new System.Windows.Data.Binding("Value")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            FrameworkElementFactory CreatePickButtonFactory()
            {
                var factory = new FrameworkElementFactory(typeof(Button));

                factory.SetValue(Button.ContentProperty, "Pick");
                factory.SetValue(Button.MinWidthProperty, 58.0);
                factory.SetValue(Button.MarginProperty, new Thickness(2));

                factory.SetBinding(Button.TagProperty, new System.Windows.Data.Binding("."));
                factory.AddHandler(Button.ClickEvent, new RoutedEventHandler(PickWorldReferenceButton_Click));
                factory.AddHandler(Button.LoadedEvent, new RoutedEventHandler(WorldReferencePickButton_Loaded));

                return factory;
            }

            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Pick",
                CellTemplate = new DataTemplate { VisualTree = CreatePickButtonFactory() },
                Width = new DataGridLength(75)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Type",
                Binding = new System.Windows.Data.Binding("Type"),
                IsReadOnly = true,
                Width = new DataGridLength(90)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Known",
                Binding = new System.Windows.Data.Binding("KnownText"),
                IsReadOnly = true,
                Width = new DataGridLength(90)
            });
        }

        private void CommitWorldGrids()
        {
            Keyboard.ClearFocus();

            foreach (DataGrid grid in new[]
            {
        WorldGeneralGrid,
        WorldMapGrid,
        WorldPlayerGrid,
        WorldMobGrid,
        WorldItemGrid,
        WorldGuildGrid,
        WorldCtfGrid,
        WorldMiscGrid
    })
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }

        private void LoadWorldRowsIntoTabs(List<WorldSettingRow> rows)
        {
            _worldRows = new ObservableCollection<WorldSettingRow>(rows);

            WorldGeneralGrid.ItemsSource = _worldRows.Where(row => WorldSettingGroup(row.Key) == "General").ToList();
            WorldMapGrid.ItemsSource = _worldRows.Where(row => WorldSettingGroup(row.Key) == "Map").ToList();
            WorldPlayerGrid.ItemsSource = _worldRows.Where(row => WorldSettingGroup(row.Key) == "Player").ToList();
            WorldMobGrid.ItemsSource = _worldRows.Where(row => WorldSettingGroup(row.Key) == "Mob").ToList();
            WorldItemGrid.ItemsSource = _worldRows.Where(row => WorldSettingGroup(row.Key) == "Item").ToList();
            WorldGuildGrid.ItemsSource = _worldRows.Where(row => WorldSettingGroup(row.Key) == "Guild").ToList();
            WorldCtfGrid.ItemsSource = _worldRows.Where(row => WorldSettingGroup(row.Key) == "CTF").ToList();
            WorldMiscGrid.ItemsSource = _worldRows.Where(row => WorldSettingGroup(row.Key) == "Misc").ToList();
        }

        private static string WorldSettingGroup(string key)
        {
            if (WorldGeneralFields.Contains(key))
                return "General";

            if (WorldMapFields.Contains(key))
                return "Map";

            if (WorldPlayerFields.Contains(key))
                return "Player";

            if (WorldMobFields.Contains(key))
                return "Mob";

            if (WorldItemFields.Contains(key))
                return "Item";

            if (WorldGuildFields.Contains(key))
                return "Guild";

            if (WorldCtfFields.Contains(key))
                return "CTF";

            return "Misc";
        }

        private static readonly HashSet<string> WorldGeneralFields = new(StringComparer.OrdinalIgnoreCase)
{
    "OwnerEmail",
    "ServerMessage",
    "LogThreshold",
    "GlobalChatDelay",
    "PostDelay",
    "ServerPost",
    "PPSLimit",
    "AllowDupIPClient",
    "MaxDupIP",
    "XPFactorCombat",
    "XPFactorTrade",
    "UltraAdmin",
    "SecretClient",
    "PopupMOTD",
    "LogHistory",
    "Perks",
    "AllowImageChange",
    "AllowTopTen",
    "DisableClientSecurity",
    "DisableOptimizedMonsterMoves",
    "Bitch",
    "NoSeasons",
    "DisableNewbieIsland",
    "OpenReservedLand",
    "UseableUnclaimedLand",
    "UsableUnclaimedLand",
    "CaveInItemDestroy"
};

        private static readonly HashSet<string> WorldMapFields = new(StringComparer.OrdinalIgnoreCase)
{
    "MapSize",
    "WorldDepth",
    "StartingXPos",
    "StartingYPos",
    "StartingZPos",
    "ClimbLow",
    "ClimbHigh",
    "SurfaceGrowth",
    "SurfaceDamage",
    "MaxUnclaimCount",
    "MaxUnlclaimCount",
    "MaxLandOwn",
    "LandClaimCost",
    "LandOwnerTimeToLiveDays",
    "PlayerSurfaceCost"
};

        private static readonly HashSet<string> WorldPlayerFields = new(StringComparer.OrdinalIgnoreCase)
{
    "SkillPoints",
    "AttributePoints",
    "SkillRollType",
    "SkillRollAlpha",
    "PKProtectionLevel",
    "PKLevelRange",
    "MaxPlayerFood",
    "MaxPlayerWater",
    "FoodOnDeath",
    "PassedOutImage",
    "MaxPlayerPerAccount",
    "VitaePenalty",
    "MaxPoison",
    "TimeToResurrect",
    "DeathProtectionTime"
};

        private static readonly HashSet<string> WorldMobFields = new(StringComparer.OrdinalIgnoreCase)
{
    "AutoStalkerID",
    "MobEffect",
    "MonsterSpread",
    "MonsterChase",
    "MonsterProcessSeconds",
    "MonsterSpawnCount",
    "DefaultMonsterSpeed",
    "MaxTameCount",
    "NPCTraderStartGold",
    "TraderBuyMax",
    "TraderBuyCost",
    "NPCIdleTimeout",
    "TameShareXPPercent"
};

        private static readonly HashSet<string> WorldItemFields = new(StringComparer.OrdinalIgnoreCase)
{
    "ItemMapStackLimit",
    "ItemOwnerDecay",
    "ItemOwnerDexay",
    "MaxItemSkillBonus",
    "MeteoriteItem",
    "MiningBraceSurface",
    "MiningDepthMax",
    "MiningDepthFactor"
};

        private static readonly HashSet<string> WorldGuildFields = new(StringComparer.OrdinalIgnoreCase)
{
    "GuildCreateCost",
    "GuildeCreateCost",
    "GuildCreateLevel",
    "GuildeCreateLevel",
    "GuildMaintainCost",
    "GuildLandClaimCost"
};

        private static readonly HashSet<string> WorldCtfFields = new(StringComparer.OrdinalIgnoreCase)
{
    "CTFRedImage",
    "CTFBlueImage",
    "CTFRedResurrect",
    "CTFBlueResurrect",
    "CTFRedRessurect",
    "CTFBlueRessurect"
};

        private void SaveWorld()
        {
            if (string.IsNullOrWhiteSpace(_worldIniPath) ||
                !File.Exists(_worldIniPath))
            {
                StatusText.Text = "world.ini was not found.";
                return;
            }

            try
            {
                CommitWorldGrids();

                SaveValidationResult result = _worldEditorService.SaveRows(
                    _worldIniPath,
                    _worldRows.ToList());

                if (!result.Success)
                {
                    StatusText.Text = $"Save failed: {result.ErrorMessage}";

                    System.Windows.MessageBox.Show(
                        result.ErrorMessage ?? "Save failed.",
                        "Save failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                StatusText.Text =
                    $"{result.Message} Backup: {result.BackupPath}";

                System.Windows.MessageBox.Show(
                    $"{result.Message}\n\nBackup created:\n{result.BackupPath}",
                    "World saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ReloadWorkspaceAfterSave();

                ClearWorldEditorOnly();
                LoadWorldRowsIfNeeded();

                if (!string.IsNullOrWhiteSpace(_worldIniPath))
                    WorldRawPreviewText.Text = _previewService.GetWorldPreview(_worldIniPath);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Save world.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadWorldRowsIfNeeded()
        {
            if (WorldGeneralGrid.ItemsSource is not null)
                return;

            if (string.IsNullOrWhiteSpace(_worldIniPath) ||
                !File.Exists(_worldIniPath))
            {
                StatusText.Text = "world.ini was not found.";
                return;
            }

            try
            {
                List<WorldSettingRow> rows = _worldEditorService.LoadRows(_worldIniPath);

                LoadWorldRowsIntoTabs(rows);

                WorldRawPreviewText.Text = _previewService.GetWorldPreview(_worldIniPath);

                StatusText.Text = $"Loaded world.ini editor with {_worldRows.Count} row(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load world.ini: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load world.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ClearWorldEditorOnly()
        {
            WorldGeneralGrid.ItemsSource = null;
            WorldMapGrid.ItemsSource = null;
            WorldPlayerGrid.ItemsSource = null;
            WorldMobGrid.ItemsSource = null;
            WorldItemGrid.ItemsSource = null;
            WorldGuildGrid.ItemsSource = null;
            WorldCtfGrid.ItemsSource = null;
            WorldMiscGrid.ItemsSource = null;

            _worldRows.Clear();

            WorldRawPreviewText.Text = "";
        }

        private void WorldReferencePickButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is WorldSettingRow row && IsWorldReferencePickerField(row.Key))
            {
                button.Visibility = Visibility.Visible;
                button.IsEnabled = true;
            }
            else
            {
                button.Visibility = Visibility.Collapsed;
                button.IsEnabled = false;
            }
        }

        private void PickWorldReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.Tag is not WorldSettingRow row)
            {
                StatusText.Text = "Could not determine which world field to edit.";
                return;
            }

            if (IsWorldItemReferenceField(row.Key))
            {
                OpenWorldItemReferencePicker(row);
                return;
            }

            if (IsWorldMonsterReferenceField(row.Key))
            {
                OpenWorldMonsterReferencePicker(row);
                return;
            }

            if (IsWorldSingleSpriteReferenceField(row.Key))
            {
                OpenWorldSpritePicker(row, append: false);
                return;
            }

            if (IsWorldMultiSpriteReferenceField(row.Key))
            {
                OpenWorldSpritePicker(row, append: true);
                return;
            }

            StatusText.Text = $"{row.Key} does not have a picker.";
        }
        private void OpenWorldItemReferencePicker(WorldSettingRow targetRow)
        {
            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
            {
                StatusText.Text = "item.ini was not found.";
                MessageBox.Show("item.ini was not found.", "Item picker", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<ItemRow> items = _itemEditorService.LoadItemRows(_itemIniPath)
                .OrderBy(item => item.Name)
                .ThenBy(item => item.Id)
                .ToList();

            var picker = new Window
            {
                Title = $"Pick item for {targetRow.Key}",
                Owner = this,
                Width = 850,
                Height = 650,
                MinWidth = 620,
                MinHeight = 460,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel { Margin = new Thickness(10) };
            var searchBox = new TextBox { Margin = new Thickness(0, 0, 0, 8) };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock { Margin = new Thickness(0, 8, 0, 0), Opacity = 0.75 };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = new DataGridLength(80) });
            grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new System.Windows.Data.Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            grid.Columns.Add(new DataGridTextColumn { Header = "Class", Binding = new System.Windows.Data.Binding("Class"), Width = new DataGridLength(120) });
            grid.Columns.Add(new DataGridTextColumn { Header = "SubType", Binding = new System.Windows.Data.Binding("SubType"), Width = new DataGridLength(140) });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<ItemRow> filtered = items;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(item =>
                        item.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.Class.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.SubType.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<ItemRow> visible = filtered.ToList();

                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {items.Count} item(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not ItemRow item)
                    return;

                targetRow.Value = item.Id.ToString();
                targetRow.Enabled = true;

                RefreshWorldGrids();

                picker.Close();

                StatusText.Text = $"Set {targetRow.Key} to Item={item.Id} ({item.Name}).";
            }

            searchBox.TextChanged += (_, _) => Refresh();
            grid.MouseDoubleClick += (_, _) => SelectCurrent();

            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }

        private void OpenWorldMonsterReferencePicker(WorldSettingRow targetRow)
        {
            if (string.IsNullOrWhiteSpace(_monsterIniPath) || !File.Exists(_monsterIniPath))
            {
                StatusText.Text = "monster.ini was not found.";
                MessageBox.Show("monster.ini was not found.", "Monster picker", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<MonsterRow> monsters = _monsterEditorService.LoadMonsterRows(_monsterIniPath)
                .OrderBy(monster => monster.Name)
                .ThenBy(monster => monster.Id)
                .ToList();

            var picker = new Window
            {
                Title = $"Pick monster for {targetRow.Key}",
                Owner = this,
                Width = 760,
                Height = 620,
                MinWidth = 560,
                MinHeight = 440,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel { Margin = new Thickness(10) };
            var searchBox = new TextBox { Margin = new Thickness(0, 0, 0, 8) };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock { Margin = new Thickness(0, 8, 0, 0), Opacity = 0.75 };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = new DataGridLength(80) });
            grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new System.Windows.Data.Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            grid.Columns.Add(new DataGridTextColumn { Header = "Fields", Binding = new System.Windows.Data.Binding("FieldCount"), Width = new DataGridLength(80) });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<MonsterRow> filtered = monsters;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(monster =>
                        monster.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        monster.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<MonsterRow> visible = filtered.ToList();

                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {monsters.Count} monster(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not MonsterRow monster)
                    return;

                targetRow.Value = monster.Id.ToString();
                targetRow.Enabled = true;

                RefreshWorldGrids();

                picker.Close();

                StatusText.Text = $"Set {targetRow.Key} to Monster={monster.Id} ({monster.Name}).";
            }

            searchBox.TextChanged += (_, _) => Refresh();
            grid.MouseDoubleClick += (_, _) => SelectCurrent();

            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }

        private void OpenWorldSpritePicker(WorldSettingRow targetRow, bool append)
        {
            if (string.IsNullOrWhiteSpace(_serverFolderPath))
            {
                StatusText.Text = "Open a server folder before picking sprites.";
                return;
            }

            string spritesFolder = Path.Combine(_serverFolderPath, "Sprites");

            if (!Directory.Exists(spritesFolder))
            {
                StatusText.Text = $"Sprites folder not found: {spritesFolder}";

                MessageBox.Show(
                    $"Could not find the Sprites folder:\n\n{spritesFolder}",
                    "Sprites folder missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            List<ItemSpriteTile> tiles = LoadSpriteTilesByPrefix(spritesFolder, "player");

            if (tiles.Count == 0)
            {
                StatusText.Text = "No player spritesheets were found.";

                MessageBox.Show(
                    $"No usable player sprite sheets were found in:\n\n{spritesFolder}\n\nExpected names like player0.bmp, player1.bmp, player2.png, etc.",
                    "No sprites found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var picker = new Window
            {
                Title = append
                    ? $"Pick sprite to append for {targetRow.Key}"
                    : $"Pick sprite for {targetRow.Key}",
                Owner = this,
                Width = 1200,
                Height = 800,
                MinWidth = 900,
                MinHeight = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel { Margin = new Thickness(10) };

            var topPanel = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchLabel = new TextBlock { Text = "Search ID:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            var searchBox = new TextBox { MinWidth = 140, Margin = new Thickness(0, 0, 12, 0) };
            var sheetLabel = new TextBlock { Text = "Sheet:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            var sheetFilter = new ComboBox { Margin = new Thickness(0, 0, 12, 0) };
            var closeButton = new Button { Content = "Close", MinWidth = 90 };

            Grid.SetColumn(searchLabel, 0);
            Grid.SetColumn(searchBox, 1);
            Grid.SetColumn(sheetLabel, 2);
            Grid.SetColumn(sheetFilter, 3);
            Grid.SetColumn(closeButton, 4);

            topPanel.Children.Add(searchLabel);
            topPanel.Children.Add(searchBox);
            topPanel.Children.Add(sheetLabel);
            topPanel.Children.Add(sheetFilter);
            topPanel.Children.Add(closeButton);

            DockPanel.SetDock(topPanel, Dock.Top);
            root.Children.Add(topPanel);

            var statusText = new TextBlock { Margin = new Thickness(0, 8, 0, 0) };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var wrap = new WrapPanel { Orientation = Orientation.Horizontal };

            scroll.Content = wrap;
            root.Children.Add(scroll);

            picker.Content = root;

            sheetFilter.Items.Add("All");

            foreach (string sheetName in tiles.Select(tile => tile.SheetName).Distinct().OrderBy(value => value))
                sheetFilter.Items.Add(sheetName);

            sheetFilter.SelectedItem = "All";

            void RenderTiles()
            {
                wrap.Children.Clear();

                string search = searchBox.Text.Trim();
                string selectedSheet = sheetFilter.SelectedItem?.ToString() ?? "All";

                IEnumerable<ItemSpriteTile> filtered = tiles;

                if (!string.IsNullOrWhiteSpace(search))
                    filtered = filtered.Where(tile => tile.SpriteId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));

                if (!string.Equals(selectedSheet, "All", StringComparison.OrdinalIgnoreCase))
                    filtered = filtered.Where(tile => string.Equals(tile.SheetName, selectedSheet, StringComparison.OrdinalIgnoreCase));

                List<ItemSpriteTile> visibleTiles = filtered.ToList();

                foreach (ItemSpriteTile tile in visibleTiles)
                {
                    var image = new Image
                    {
                        Source = tile.Image,
                        Width = 32,
                        Height = 32,
                        Stretch = Stretch.None,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var idText = new TextBlock
                    {
                        Text = tile.SpriteId.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 11
                    };

                    var stack = new StackPanel { Orientation = Orientation.Vertical };
                    stack.Children.Add(image);
                    stack.Children.Add(idText);

                    var button = new Button
                    {
                        Content = stack,
                        Width = 62,
                        Height = 68,
                        Margin = new Thickness(3),
                        ToolTip = $"{tile.SheetName} | ID {tile.SpriteId}"
                    };

                    button.Click += (_, _) =>
                    {
                        string spriteText = tile.SpriteId.ToString();

                        if (append && !string.IsNullOrWhiteSpace(targetRow.Value))
                            targetRow.Value = $"{targetRow.Value.Trim()},{spriteText}";
                        else
                            targetRow.Value = spriteText;

                        targetRow.Enabled = true;

                        RefreshWorldGrids();

                        picker.Close();

                        StatusText.Text = append
                            ? $"Added sprite ID {spriteText} to {targetRow.Key}."
                            : $"Set {targetRow.Key} to sprite ID {spriteText}.";
                    };

                    wrap.Children.Add(button);
                }

                statusText.Text = $"Showing {visibleTiles.Count} of {tiles.Count} sprite(s).";
            }

            searchBox.TextChanged += (_, _) => RenderTiles();
            sheetFilter.SelectionChanged += (_, _) => RenderTiles();
            closeButton.Click += (_, _) => picker.Close();

            RenderTiles();
            picker.ShowDialog();
        }

        private void RefreshWorldGrids()
        {
            WorldGeneralGrid.Items.Refresh();
            WorldMapGrid.Items.Refresh();
            WorldPlayerGrid.Items.Refresh();
            WorldMobGrid.Items.Refresh();
            WorldItemGrid.Items.Refresh();
            WorldGuildGrid.Items.Refresh();
            WorldCtfGrid.Items.Refresh();
            WorldMiscGrid.Items.Refresh();
        }

        #endregion
        #region Treasure
        private string? _treasureIniPath;

        private ObservableCollection<TreasureRow> _treasureRows = new();
        private ObservableCollection<TreasureRow> _filteredTreasureRows = new();
        private ObservableCollection<TreasureFieldRow> _treasureFieldRows = new();
        private int? _selectedTreasureId;

        private void LoadTreasureRowsIfNeeded()
        {
            if (TreasureListGrid.ItemsSource is not null)
                return;

            if (string.IsNullOrWhiteSpace(_treasureIniPath) ||
                !File.Exists(_treasureIniPath))
            {
                StatusText.Text = "treasure.ini was not found.";
                return;
            }

            try
            {
                List<TreasureRow> rows = _treasureEditorService.LoadTreasureRows(_treasureIniPath);

                _treasureRows = new ObservableCollection<TreasureRow>(rows);
                _filteredTreasureRows = new ObservableCollection<TreasureRow>(rows);

                TreasureListGrid.ItemsSource = _filteredTreasureRows;
                TreasureEntryGrid.ItemsSource = null;
                TreasureEntryFieldGrid.ItemsSource = null;

                _treasureEntryRows.Clear();
                _selectedTreasureEntryFieldRows.Clear();

                _selectedTreasureId = null;
                _selectedTreasureEntryIndex = null;

                SelectedTreasureTableText.Text = "No treasure table selected.";
                SelectedTreasureEntryText.Text = "No treasure entry selected.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load treasure.ini: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load treasure.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static List<TreasureFieldRow> BuildTreasureEntryFieldRows(TreasureEntryRow entry)
        {
            return new List<TreasureFieldRow>
            {
                new() { Type = "Field", Key = "Item", Value = entry.Item, Enabled = !string.IsNullOrWhiteSpace(entry.Item), IsKnown = true },
                new() { Type = "Field", Key = "SkillId", Value = entry.SkillId, Enabled = !string.IsNullOrWhiteSpace(entry.SkillId), IsKnown = true },
                new() { Type = "Field", Key = "SkillLow", Value = entry.SkillLow, Enabled = !string.IsNullOrWhiteSpace(entry.SkillLow), IsKnown = true },
                new() { Type = "Field", Key = "SkillHigh", Value = entry.SkillHigh, Enabled = !string.IsNullOrWhiteSpace(entry.SkillHigh), IsKnown = true },
                new() { Type = "Field", Key = "Chance", Value = entry.Chance, Enabled = !string.IsNullOrWhiteSpace(entry.Chance), IsKnown = true },
                new() { Type = "Field", Key = "SpellID", Value = entry.SpellID, Enabled = !string.IsNullOrWhiteSpace(entry.SpellID), IsKnown = true },
                new() { Type = "Field", Key = "SpellData", Value = entry.SpellData, Enabled = !string.IsNullOrWhiteSpace(entry.SpellData), IsKnown = true },
            };
        }

        private void NewTreasureEntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTreasureId is null)
            {
                StatusText.Text = "Select a treasure table before adding an entry.";
                return;
            }

            int nextIndex = _treasureEntryRows.Any()
                ? _treasureEntryRows.Max(row => row.EntryIndex) + 1
                : 1;

            var entry = new TreasureEntryRow
            {
                EntryIndex = nextIndex,
                Item = "",
                SkillId = "",
                SkillLow = "",
                SkillHigh = "",
                Chance = "",
                SpellID = "",
                SpellData = "",
                FieldCount = 0
            };

            _treasureEntryRows.Add(entry);
            TreasureEntryGrid.Items.Refresh();

            TreasureEntryGrid.SelectedItem = entry;
            _selectedTreasureEntryIndex = entry.EntryIndex;

            _selectedTreasureEntryFieldRows = new ObservableCollection<TreasureFieldRow>(
                BuildTreasureEntryFieldRows(entry));

            TreasureEntryFieldGrid.ItemsSource = _selectedTreasureEntryFieldRows;

            SelectedTreasureEntryText.Text = $"Entry {entry.EntryIndex}: New Entry";
            StatusText.Text = $"Created treasure entry {entry.EntryIndex}. Commit and save to write it.";
        }

        private void DeleteTreasureEntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (TreasureEntryGrid.SelectedItem is not TreasureEntryRow entry)
            {
                StatusText.Text = "Select a treasure entry before deleting.";
                return;
            }

            MessageBoxResult confirm = MessageBox.Show(
                $"Delete treasure entry {entry.EntryIndex}?",
                "Delete treasure entry",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            _treasureEntryRows.Remove(entry);

            int index = 1;

            foreach (TreasureEntryRow row in _treasureEntryRows.OrderBy(row => row.EntryIndex))
            {
                row.EntryIndex = index;
                index++;
            }

            TreasureEntryGrid.Items.Refresh();

            _selectedTreasureEntryIndex = null;
            _selectedTreasureEntryFieldRows.Clear();
            TreasureEntryFieldGrid.ItemsSource = null;

            SelectedTreasureEntryText.Text = "No treasure entry selected.";
            StatusText.Text = "Deleted treasure entry. Use Save to write treasure.ini.";
        }

        private void SaveTreasure()
        {
            StatusText.Text = "Treasure entry editing is wired. Next step is writing the three-panel entries back to treasure.ini.";
            MessageBox.Show(
                "Treasure entry editing is wired.\n\nNext step is writing the selected treasure table entries back to treasure.ini.",
                "Treasure save not wired yet",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        private void OpenTreasureItemReferencePicker(TreasureFieldRow targetRow)
        {
            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
            {
                StatusText.Text = "item.ini was not found.";
                MessageBox.Show("item.ini was not found.", "Item picker", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<ItemRow> items;

            try
            {
                items = _itemEditorService.LoadItemRows(_itemIniPath)
                    .OrderBy(item => item.Name)
                    .ThenBy(item => item.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load items: {ex.Message}";
                MessageBox.Show(ex.Message, "Item picker failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var picker = new Window
            {
                Title = "Pick treasure item",
                Owner = this,
                Width = 850,
                Height = 650,
                MinWidth = 620,
                MinHeight = 460,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel { Margin = new Thickness(10) };

            var searchBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Opacity = 0.75
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("Id"),
                Width = new DataGridLength(80)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new System.Windows.Data.Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Class",
                Binding = new System.Windows.Data.Binding("Class"),
                Width = new DataGridLength(120)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "SubType",
                Binding = new System.Windows.Data.Binding("SubType"),
                Width = new DataGridLength(140)
            });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<ItemRow> filtered = items;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(item =>
                        item.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.Class.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.SubType.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<ItemRow> visible = filtered.ToList();

                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {items.Count} item(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not ItemRow item)
                    return;

                targetRow.Value = item.Name;
                targetRow.Enabled = true;

                TreasureEntryFieldGrid.Items.Refresh();

                picker.Close();

                StatusText.Text = $"Set treasure Item to {item.Name}.";
            }

            searchBox.TextChanged += (_, _) => Refresh();
            grid.MouseDoubleClick += (_, _) => SelectCurrent();

            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }

        private void OpenTreasureSkillReferencePicker(TreasureFieldRow targetRow)
        {
            if (string.IsNullOrWhiteSpace(_skillIniPath) || !File.Exists(_skillIniPath))
            {
                StatusText.Text = "skill.ini was not found.";
                MessageBox.Show("skill.ini was not found.", "Skill picker", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<SkillRow> skills;

            try
            {
                skills = _skillEditorService.LoadSkillRows(_skillIniPath)
                    .OrderBy(skill => skill.Name)
                    .ThenBy(skill => skill.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load skills: {ex.Message}";
                MessageBox.Show(ex.Message, "Skill picker failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var picker = new Window
            {
                Title = "Pick treasure skill",
                Owner = this,
                Width = 700,
                Height = 600,
                MinWidth = 520,
                MinHeight = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel { Margin = new Thickness(10) };

            var searchBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Opacity = 0.75
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("Id"),
                Width = new DataGridLength(80)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new System.Windows.Data.Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<SkillRow> filtered = skills;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(skill =>
                        skill.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        skill.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<SkillRow> visible = filtered.ToList();

                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {skills.Count} skill(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not SkillRow skill)
                    return;

                targetRow.Value = skill.Name;
                targetRow.Enabled = true;

                TreasureEntryFieldGrid.Items.Refresh();

                picker.Close();

                StatusText.Text = $"Set treasure SkillId to {skill.Name}.";
            }

            searchBox.TextChanged += (_, _) => Refresh();
            grid.MouseDoubleClick += (_, _) => SelectCurrent();

            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }

        private void RefreshTreasureFilter()
        {
            string needle = TreasureSearchBox.Text.Trim();

            IEnumerable<TreasureRow> filtered = _treasureRows;

            if (!string.IsNullOrWhiteSpace(needle))
            {
                filtered = filtered.Where(treasure =>
                    treasure.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    treasure.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            _filteredTreasureRows = new ObservableCollection<TreasureRow>(filtered);

            TreasureListGrid.ItemsSource = _filteredTreasureRows;
        }

        private void TreasureSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TreasureListGrid is null)
                return;

            RefreshTreasureFilter();
        }

        private void TreasureListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TreasureListGrid.SelectedItem is not TreasureRow selectedTreasure)
                return;

            LoadSelectedTreasureFields(selectedTreasure.Id);
        }

        private void LoadSelectedTreasureFields(int treasureId)
        {
            if (string.IsNullOrWhiteSpace(_treasureIniPath) ||
                !File.Exists(_treasureIniPath))
            {
                StatusText.Text = "treasure.ini was not found.";
                return;
            }

            try
            {
                List<TreasureEntryRow> entries = _treasureEditorService.LoadEntryRows(
                    _treasureIniPath,
                    treasureId);

                _selectedTreasureId = treasureId;
                _selectedTreasureEntryIndex = null;

                _treasureEntryRows = new ObservableCollection<TreasureEntryRow>(entries);
                _selectedTreasureEntryFieldRows.Clear();

                TreasureEntryGrid.ItemsSource = _treasureEntryRows;
                TreasureEntryFieldGrid.ItemsSource = null;

                TreasureRow? treasure = _treasureRows.FirstOrDefault(row => row.Id == treasureId);

                SelectedTreasureTableText.Text = treasure is null
                    ? $"Treasure {treasureId}"
                    : $"Treasure {treasure.Id}: {treasure.Name}";

                SelectedTreasureTableText.Text = treasure is null
                    ? $"Treasure {treasureId}"
                    : $"Treasure {treasure.Id}: {treasure.Name}";

                SelectedTreasureEntryText.Text = "No treasure entry selected.";

                SaveButton.IsEnabled = true;

                StatusText.Text = $"Loaded Treasure={treasureId} with {_treasureEntryRows.Count} treasure entrie(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load Treasure={treasureId}: {ex.Message}";

                MessageBox.Show(
                    ex.Message,
                    "Load treasure failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void TreasureEntryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TreasureEntryGrid.SelectedItem is not TreasureEntryRow entry)
                return;

            _selectedTreasureEntryIndex = entry.EntryIndex;

            List<TreasureFieldRow> fields = BuildTreasureEntryFieldRows(entry);

            _selectedTreasureEntryFieldRows = new ObservableCollection<TreasureFieldRow>(fields);
            TreasureEntryFieldGrid.ItemsSource = _selectedTreasureEntryFieldRows;

            SelectedTreasureEntryText.Text = $"Entry {entry.EntryIndex}: {entry.DisplayName}";
        }

        private static bool IsTreasureItemReferenceField(string key)
        {
            return key.Equals("Item", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTreasureSkillReferenceField(string key)
        {
            return key.Equals("SkillId", StringComparison.OrdinalIgnoreCase);
        }

        private void TreasureReferencePickButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is TreasureFieldRow row &&
                (IsTreasureItemReferenceField(row.Key) || IsTreasureSkillReferenceField(row.Key)))
            {
                button.Visibility = Visibility.Visible;
                button.IsEnabled = true;
            }
            else
            {
                button.Visibility = Visibility.Collapsed;
                button.IsEnabled = false;
            }
        }

        private void PickTreasureReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.Tag is not TreasureFieldRow row)
            {
                StatusText.Text = "Could not determine which treasure field to edit.";
                return;
            }

            if (IsTreasureItemReferenceField(row.Key))
            {
                OpenTreasureItemReferencePicker(row);
                return;
            }

            if (IsTreasureSkillReferenceField(row.Key))
            {
                OpenTreasureSkillReferencePicker(row);
                return;
            }

            StatusText.Text = $"{row.Key} does not have a picker.";
        }

        private void CommitTreasureEntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTreasureEntryIndex is null)
            {
                StatusText.Text = "Select a treasure entry before committing.";
                return;
            }

            TreasureEntryFieldGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            TreasureEntryFieldGrid.CommitEdit(DataGridEditingUnit.Row, true);

            TreasureEntryRow? entry = _treasureEntryRows.FirstOrDefault(row =>
                row.EntryIndex == _selectedTreasureEntryIndex.Value);

            if (entry is null)
            {
                StatusText.Text = "Could not find selected treasure entry.";
                return;
            }

            foreach (TreasureFieldRow field in _selectedTreasureEntryFieldRows)
            {
                string value = field.Enabled ? field.Value?.Trim() ?? "" : "";

                switch (field.Key.ToLowerInvariant())
                {
                    case "item":
                        entry.Item = value;
                        break;
                    case "skillid":
                        entry.SkillId = value;
                        break;
                    case "skilllow":
                        entry.SkillLow = value;
                        break;
                    case "skillhigh":
                        entry.SkillHigh = value;
                        break;
                    case "chance":
                        entry.Chance = value;
                        break;
                    case "spellid":
                        entry.SpellID = value;
                        break;
                    case "spelldata":
                        entry.SpellData = value;
                        break;
                }
            }

            TreasureEntryGrid.Items.Refresh();

            StatusText.Text = $"Committed treasure entry {entry.EntryIndex}. Use Save to write treasure.ini.";
        }

        private void ShowTreasureView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Collapsed;
            TreasureView.Visibility = Visibility.Visible;
            MagicView.Visibility = Visibility.Collapsed;
            MonsterView.Visibility = Visibility.Collapsed;
            ItemView.Visibility = Visibility.Collapsed;
            UsageView.Visibility = Visibility.Collapsed;
            MultiUseView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;

            ExportCsvButton.IsEnabled = _treasureIniPath is not null && File.Exists(_treasureIniPath);

            LoadTreasureRowsIfNeeded();

            SaveButton.IsEnabled = TreasureEntryGrid.ItemsSource is not null;

            StatusText.Text = "Treasure editor loaded.";
        }


        #endregion

        #region Magic
        private string? _magicIniPath;

        private ObservableCollection<MagicRow> _magicRows = new();
        private ObservableCollection<MagicRow> _filteredMagicRows = new();
        private ObservableCollection<MagicFieldRow> _magicFieldRows = new();
        private int? _selectedMagicId;
        private static readonly HashSet<string> MagicSkillReferenceFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "Skill"
        };
        private static readonly HashSet<string> MagicItemReferenceFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "Rune1",
            "Rune2",
            "Rune3",
            "Rune4",
            "Rune5"
        };
        private static readonly HashSet<string> MagicAnimationReferenceFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "Animation"
        };

        private static bool IsMagicSkillReferenceField(string fieldName)
        {
            return MagicSkillReferenceFields.Contains(fieldName);
        }

        private void OpenMagicSkillReferencePicker(string targetField)
        {
            if (string.IsNullOrWhiteSpace(_skillIniPath) || !File.Exists(_skillIniPath))
            {
                StatusText.Text = "skill.ini was not found.";

                MessageBox.Show(
                    "skill.ini was not found.",
                    "Skill picker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            List<SkillRow> skills;

            try
            {
                skills = _skillEditorService.LoadSkillRows(_skillIniPath)
                    .OrderBy(skill => skill.Name)
                    .ThenBy(skill => skill.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load skills: {ex.Message}";

                MessageBox.Show(
                    ex.Message,
                    "Skill picker failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var picker = new Window
            {
                Title = $"Pick skill for {targetField}",
                Owner = this,
                Width = 700,
                Height = 600,
                MinWidth = 520,
                MinHeight = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel
            {
                Margin = new Thickness(10)
            };

            var searchBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Opacity = 0.75
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("Id"),
                Width = new DataGridLength(80)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new System.Windows.Data.Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<SkillRow> filtered = skills;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(skill =>
                        skill.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        skill.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<SkillRow> visible = filtered.ToList();

                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {skills.Count} skill(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not SkillRow skill)
                    return;

                SetMagicFieldValue(targetField, skill.Name, enableField: true);
                LoadMagicFieldRowsIntoTabs(_magicFieldRows.ToList());

                picker.Close();

                StatusText.Text = $"Set {targetField} to {skill.Name}.";
            }

            searchBox.TextChanged += (_, _) => Refresh();

            grid.MouseDoubleClick += (_, _) => SelectCurrent();

            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }

        private static bool IsMagicAnimationReferenceField(string fieldName)
        {
            return MagicAnimationReferenceFields.Contains(fieldName);
        }

        private static bool IsMagicReferencePickerField(string fieldName)
        {
            return IsMagicSkillReferenceField(fieldName)
                || IsMagicItemReferenceField(fieldName)
                || IsMagicAnimationReferenceField(fieldName);
        }
        private void SaveMagic()
        {
            if (string.IsNullOrWhiteSpace(_magicIniPath) ||
                !File.Exists(_magicIniPath))
            {
                StatusText.Text = "magic.ini was not found.";
                return;
            }

            if (_selectedMagicId is null)
            {
                StatusText.Text = "Select a spell before saving.";
                return;
            }

            try
            {
                CommitMagicGrids();
                NormalizeMagicItemReferenceFieldsBeforeSave();
                SaveValidationResult result = _magicEditorService.SaveMagicRows(
                    _magicIniPath,
                    _selectedMagicId.Value,
                    _magicFieldRows.ToList());

                if (!result.Success)
                {
                    StatusText.Text = $"Save failed: {result.ErrorMessage}";

                    System.Windows.MessageBox.Show(
                        result.ErrorMessage ?? "Save failed.",
                        "Save failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                StatusText.Text =
                    $"{result.Message} Backup: {result.BackupPath}";

                System.Windows.MessageBox.Show(
                    $"{result.Message}\n\nBackup created:\n{result.BackupPath}",
                    "Magic saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                int savedMagicId = _selectedMagicId.Value;

                ReloadWorkspaceAfterSave();

                MagicListGrid.ItemsSource = null;
                ClearMagicEditorOnly();

                LoadMagicRowsIfNeeded();

                MagicRow? savedRow = _filteredMagicRows.FirstOrDefault(row => row.Id == savedMagicId);

                if (savedRow is not null)
                {
                    MagicListGrid.SelectedItem = savedRow;
                    LoadSelectedMagicFields(savedMagicId);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Save magic.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void NormalizeMagicItemReferenceFieldsBeforeSave()
        {
            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
                return;

            List<ItemRow> items = _itemEditorService.LoadItemRows(_itemIniPath);

            foreach (MagicFieldRow row in _magicFieldRows.Where(row => IsMagicItemReferenceField(row.Label)))
            {
                string raw = row.Value?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                if (int.TryParse(raw, out _))
                    continue;

                ItemRow? match = items.FirstOrDefault(item =>
                    item.Name.Equals(raw, StringComparison.OrdinalIgnoreCase));

                if (match is null)
                    continue;

                row.Value = match.Id.ToString();
                row.Enabled = true;
            }
        }
        private void ShowMagicView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Collapsed;
            TreasureView.Visibility = Visibility.Collapsed;
            MagicView.Visibility = Visibility.Visible;
            MonsterView.Visibility = Visibility.Collapsed;
            ItemView.Visibility = Visibility.Collapsed;
            UsageView.Visibility = Visibility.Collapsed;
            MultiUseView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;

            ExportCsvButton.IsEnabled = _magicIniPath is not null && File.Exists(_magicIniPath);

            LoadMagicRowsIfNeeded();
            EnsureMagicFieldGridsConfigured();
            SaveButton.IsEnabled = MagicGeneralFieldGrid.ItemsSource is not null;

            StatusText.Text = "Magic editor loaded.";
        }
        private void LoadMagicRowsIfNeeded()
        {
            if (MagicListGrid.ItemsSource is not null)
                return;

            if (string.IsNullOrWhiteSpace(_magicIniPath) ||
                !File.Exists(_magicIniPath))
            {
                StatusText.Text = "magic.ini was not found.";
                return;
            }

            try
            {
                List<MagicRow> rows = _magicEditorService.LoadMagicRows(_magicIniPath);

                _magicRows = new ObservableCollection<MagicRow>(rows);
                _filteredMagicRows = new ObservableCollection<MagicRow>(rows);

                MagicListGrid.ItemsSource = _filteredMagicRows;
                ClearMagicEditorOnly();
                _selectedMagicId = null;
                SelectedMagicText.Text = "No spell selected.";

                StatusText.Text = $"Loaded magic.ini editor with {_magicRows.Count} spell(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load magic.ini: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load magic.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void RefreshMagicFilter()
        {
            string needle = MagicSearchBox.Text.Trim();

            IEnumerable<MagicRow> filtered = _magicRows;

            if (!string.IsNullOrWhiteSpace(needle))
            {
                filtered = filtered.Where(spell =>
                    spell.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    spell.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    spell.Skill.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            _filteredMagicRows = new ObservableCollection<MagicRow>(filtered);

            MagicListGrid.ItemsSource = _filteredMagicRows;
        }
        private void MagicSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MagicListGrid is null)
                return;

            RefreshMagicFilter();
        }
        private void MagicListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MagicListGrid.SelectedItem is not MagicRow selectedSpell)
                return;

            LoadSelectedMagicFields(selectedSpell.Id);
        }
        private void LoadSelectedMagicFields(int spellId)
        {
            if (string.IsNullOrWhiteSpace(_magicIniPath) ||
                !File.Exists(_magicIniPath))
            {
                StatusText.Text = "magic.ini was not found.";
                return;
            }

            try
            {
                List<MagicFieldRow> rows = _magicEditorService.LoadFieldRows(
                    _magicIniPath,
                    spellId);

                _selectedMagicId = spellId;
                LoadMagicFieldRowsIntoTabs(rows);
                MagicRawPreviewText.Text = _previewService.GetMagicPreview(
                                                                _magicIniPath,
                                                                spellId);

                MagicRow? spell = _magicRows.FirstOrDefault(row => row.Id == spellId);

                SelectedMagicText.Text = spell is null
                    ? $"Spell {spellId}"
                    : $"Spell {spell.Id}: {spell.Name}";

                SaveButton.IsEnabled = true;

                StatusText.Text = $"Loaded Spell={spellId} with {_magicFieldRows.Count} editable row(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load Spell={spellId}: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load spell failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void CommitMagicButton_Click(object sender, RoutedEventArgs e)
        {
            SaveMagic();
        }
        private void NewMagicButton_Click(object sender, RoutedEventArgs e)
        {
            int nextId = _magicRows.Any()
                ? _magicRows.Max(row => row.Id) + 1
                : 1;

            _selectedMagicId = nextId;

            List<MagicFieldRow> rows = _magicEditorService.CreateBlankFieldRows(nextId);

            LoadMagicFieldRowsIntoTabs(rows);

            SelectedMagicText.Text = $"New Spell {nextId}";
            MagicRawPreviewText.Text = "";

            SaveButton.IsEnabled = true;
            StatusText.Text = $"Created new Spell={nextId}. Commit to save it.";
        }
        private void DeleteMagicButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMagicId is null)
            {
                StatusText.Text = "Select a spell before deleting.";
                return;
            }

            int spellId = _selectedMagicId.Value;

            MessageBoxResult confirm = MessageBox.Show(
                $"Delete Spell={spellId} from magic.ini?",
                "Delete spell",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            if (string.IsNullOrWhiteSpace(_magicIniPath) || !File.Exists(_magicIniPath))
            {
                StatusText.Text = "magic.ini was not found.";
                return;
            }

            try
            {
                SaveValidationResult result = _magicEditorService.DeleteSpell(_magicIniPath, spellId);

                if (!result.Success)
                {
                    StatusText.Text = $"Delete failed: {result.ErrorMessage}";
                    MessageBox.Show(result.ErrorMessage ?? "Delete failed.", "Delete failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int deletedId = spellId;

                ReloadWorkspaceAfterSave();

                MagicListGrid.ItemsSource = null;
                ClearMagicEditorOnly();

                LoadMagicRowsIfNeeded();

                StatusText.Text = $"Deleted Spell={deletedId}. Backup: {result.BackupPath}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Delete failed: {ex.Message}";
                MessageBox.Show(ex.Message, "Delete spell failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearMagicEditorOnly()
        {
            MagicGeneralFieldGrid.ItemsSource = null;
            MagicStatsFieldGrid.ItemsSource = null;
            MagicRunesFieldGrid.ItemsSource = null;
            MagicEffectsFieldGrid.ItemsSource = null;

            _magicFieldRows.Clear();
            _selectedMagicId = null;

            SelectedMagicText.Text = "No spell selected.";
            MagicRawPreviewText.Text = "";
        }
        private static bool IsMagicItemReferenceField(string fieldName)
        {
            return MagicItemReferenceFields.Contains(fieldName);
        }
        private void EnsureMagicFieldGridsConfigured()
        {
            ConfigureMagicFieldGrid(MagicGeneralFieldGrid);
            ConfigureMagicFieldGrid(MagicStatsFieldGrid);
            ConfigureMagicFieldGrid(MagicRunesFieldGrid);
            ConfigureMagicFieldGrid(MagicEffectsFieldGrid);
        }
        private void ConfigureMagicFieldGrid(DataGrid grid)
        {
            if (grid.Columns.Count > 0)
                return;

            grid.AutoGenerateColumns = false;
            grid.CanUserAddRows = false;
            grid.CanUserDeleteRows = false;
            grid.HeadersVisibility = DataGridHeadersVisibility.Column;
            grid.SelectionMode = DataGridSelectionMode.Single;

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Field",
                Binding = new System.Windows.Data.Binding("Label"),
                IsReadOnly = true,
                Width = new DataGridLength(180)
            });

            FrameworkElementFactory CreateValueComboFactory()
            {
                var factory = new FrameworkElementFactory(typeof(ComboBox));

                factory.SetBinding(ComboBox.TextProperty, new System.Windows.Data.Binding("Value")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                });

                factory.SetBinding(ItemsControl.ItemsSourceProperty, new System.Windows.Data.Binding("Options"));
                factory.SetValue(ComboBox.IsEditableProperty, true);

                return factory;
            }

            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Value",
                CellTemplate = new DataTemplate { VisualTree = CreateValueComboFactory() },
                CellEditingTemplate = new DataTemplate { VisualTree = CreateValueComboFactory() },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            if (ReferenceEquals(grid, MagicGeneralFieldGrid) || ReferenceEquals(grid, MagicRunesFieldGrid))
            {
                FrameworkElementFactory CreateReferencePickButtonFactory()
                {
                    var factory = new FrameworkElementFactory(typeof(Button));

                    factory.SetValue(Button.ContentProperty, "Pick");
                    factory.SetValue(Button.MinWidthProperty, 58.0);
                    factory.SetValue(Button.MarginProperty, new Thickness(2));

                    factory.SetBinding(Button.TagProperty, new System.Windows.Data.Binding("Label"));
                    factory.AddHandler(Button.ClickEvent, new RoutedEventHandler(PickMagicReferenceButton_Click));
                    factory.AddHandler(Button.LoadedEvent, new RoutedEventHandler(MagicReferencePickButton_Loaded));

                    return factory;
                }

                grid.Columns.Add(new DataGridTemplateColumn
                {
                    Header = "Pick",
                    CellTemplate = new DataTemplate { VisualTree = CreateReferencePickButtonFactory() },
                    Width = new DataGridLength(75)
                });
            }
            grid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Enabled / Flag",
                Binding = new System.Windows.Data.Binding("Enabled")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                },
                Width = new DataGridLength(110)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "INI Key",
                Binding = new System.Windows.Data.Binding("IniKey"),
                IsReadOnly = true,
                Width = new DataGridLength(140)
            });
        }
        private void MagicReferencePickButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is MagicFieldRow row && IsMagicReferencePickerField(row.Label))
            {
                button.Visibility = Visibility.Visible;
                button.IsEnabled = true;
            }
            else
            {
                button.Visibility = Visibility.Collapsed;
                button.IsEnabled = false;
            }
        }
        private void PickMagicReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.Tag is not string targetField ||
                string.IsNullOrWhiteSpace(targetField))
            {
                StatusText.Text = "Could not determine which magic field to edit.";
                return;
            }

            targetField = targetField.Trim();

            if (IsMagicSkillReferenceField(targetField))
            {
                OpenMagicSkillReferencePicker(targetField);
                return;
            }

            if (IsMagicItemReferenceField(targetField))
            {
                OpenMagicItemReferencePicker(targetField);
                return;
            }

            if (IsMagicAnimationReferenceField(targetField))
            {
                OpenMagicAnimationReferencePicker(targetField);
                return;
            }

            StatusText.Text = $"{targetField} does not have a picker.";
        }
        private void SetMagicFieldValue(string label, string value, bool enableField = true)
        {
            MagicFieldRow? row = _magicFieldRows.FirstOrDefault(field =>
                string.Equals(field.Label, label, StringComparison.OrdinalIgnoreCase));

            if (row is null)
            {
                StatusText.Text = $"Could not find magic field: {label}";
                return;
            }

            row.Value = value;

            if (enableField)
                row.Enabled = true;
        }
        private void OpenMagicAnimationReferencePicker(string targetField)
        {
            if (string.IsNullOrWhiteSpace(_animationIniPath) || !File.Exists(_animationIniPath))
            {
                StatusText.Text = "animation.ini was not found.";

                MessageBox.Show(
                    "animation.ini was not found.",
                    "Animation picker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            List<AnimationRow> animations;

            try
            {
                animations = _animationEditorService.LoadAnimationRows(_animationIniPath)
                    .OrderBy(animation => animation.Name)
                    .ThenBy(animation => animation.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load animations: {ex.Message}";

                MessageBox.Show(
                    ex.Message,
                    "Animation picker failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var picker = new Window
            {
                Title = $"Pick animation for {targetField}",
                Owner = this,
                Width = 850,
                Height = 650,
                MinWidth = 620,
                MinHeight = 460,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel
            {
                Margin = new Thickness(10)
            };

            var searchBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Opacity = 0.75
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("Id"),
                Width = new DataGridLength(80)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new System.Windows.Data.Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Frames",
                Binding = new System.Windows.Data.Binding("FrameCount"),
                Width = new DataGridLength(90)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Sounds",
                Binding = new System.Windows.Data.Binding("SoundCount"),
                Width = new DataGridLength(90)
            });

            grid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Rotational",
                Binding = new System.Windows.Data.Binding("Rotational"),
                Width = new DataGridLength(95)
            });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<AnimationRow> filtered = animations;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(animation =>
                        animation.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        animation.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        animation.Summary.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<AnimationRow> visible = filtered.ToList();

                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {animations.Count} animation(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not AnimationRow animation)
                    return;

                SetMagicFieldValue(targetField, animation.Id.ToString(), enableField: true);
                LoadMagicFieldRowsIntoTabs(_magicFieldRows.ToList());

                picker.Close();

                StatusText.Text = $"Set {targetField} to Animation={animation.Id} ({animation.Name}).";
            }

            searchBox.TextChanged += (_, _) => Refresh();

            grid.MouseDoubleClick += (_, _) => SelectCurrent();

            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }
        private void OpenMagicItemReferencePicker(string targetField)
        {
            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
            {
                StatusText.Text = "item.ini was not found.";
                MessageBox.Show("item.ini was not found.", "Item picker", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<ItemRow> items;

            try
            {
                items = _itemEditorService.LoadItemRows(_itemIniPath)
                    .OrderBy(item => item.Name)
                    .ThenBy(item => item.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load items: {ex.Message}";
                MessageBox.Show(ex.Message, "Item picker failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var picker = new Window
            {
                Title = $"Pick item for {targetField}",
                Owner = this,
                Width = 850,
                Height = 650,
                MinWidth = 620,
                MinHeight = 460,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel { Margin = new Thickness(10) };

            var searchBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("Id"),
                Width = new DataGridLength(80)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new System.Windows.Data.Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Class",
                Binding = new System.Windows.Data.Binding("Class"),
                Width = new DataGridLength(120)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "SubType",
                Binding = new System.Windows.Data.Binding("SubType"),
                Width = new DataGridLength(140)
            });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<ItemRow> filtered = items;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(item =>
                        item.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.Class.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.SubType.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<ItemRow> visible = filtered.ToList();
                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {items.Count} item(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not ItemRow item)
                    return;

                SetMagicFieldValue(targetField, item.Id.ToString(), enableField: true);
                LoadMagicFieldRowsIntoTabs(_magicFieldRows.ToList());
                picker.Close();

                StatusText.Text = $"Set {targetField} to Item={item.Id} ({item.Name}).";
            }

            searchBox.TextChanged += (_, _) => Refresh();

            grid.MouseDoubleClick += (_, _) => SelectCurrent();

            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }
        private void CommitMagicGrids()
        {
            Keyboard.ClearFocus();

            foreach (DataGrid grid in new[]
            {
                MagicGeneralFieldGrid,
                MagicStatsFieldGrid,
                MagicRunesFieldGrid,
                MagicEffectsFieldGrid
            })
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }
        private void LoadMagicFieldRowsIntoTabs(List<MagicFieldRow> rows)
        {
            _magicFieldRows = new ObservableCollection<MagicFieldRow>(rows);

            MagicGeneralFieldGrid.ItemsSource = _magicFieldRows
                .Where(row => row.Group == "General")
                .ToList();

            MagicStatsFieldGrid.ItemsSource = _magicFieldRows
                .Where(row => row.Group == "Stats")
                .ToList();

            MagicRunesFieldGrid.ItemsSource = _magicFieldRows
                .Where(row => row.Group == "Runes")
                .ToList();

            MagicEffectsFieldGrid.ItemsSource = _magicFieldRows
                .Where(row => row.Group == "Effects")
                .ToList();
        }

        #endregion

        #region Monster
        private string? _monsterIniPath;

        private ObservableCollection<MonsterRow> _monsterRows = new();
        private ObservableCollection<MonsterRow> _filteredMonsterRows = new();
        private ObservableCollection<MonsterFieldRow> _monsterFieldRows = new();
        private int? _selectedMonsterId;

        private void ShowMonsterView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Collapsed;
            TreasureView.Visibility = Visibility.Collapsed;
            MagicView.Visibility = Visibility.Collapsed;
            MonsterView.Visibility = Visibility.Visible;
            ItemView.Visibility = Visibility.Collapsed;
            UsageView.Visibility = Visibility.Collapsed;
            MultiUseView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;

            ExportCsvButton.IsEnabled = _monsterIniPath is not null && File.Exists(_monsterIniPath);

            EnsureMonsterFieldGridsConfigured();
            LoadMonsterRowsIfNeeded();

            SaveButton.IsEnabled = MonsterBasicsFieldGrid.ItemsSource is not null;

            StatusText.Text = "Monster editor loaded.";
        }

        private void SaveMonster()
        {
            if (string.IsNullOrWhiteSpace(_monsterIniPath) ||
                !File.Exists(_monsterIniPath))
            {
                StatusText.Text = "monster.ini was not found.";
                return;
            }

            if (_selectedMonsterId is null)
            {
                StatusText.Text = "Select a monster before saving.";
                return;
            }

            try
            {
                CommitMonsterGrids();

                SaveValidationResult result = _monsterEditorService.SaveMonsterRows(
                    _monsterIniPath,
                    _selectedMonsterId.Value,
                    _monsterFieldRows.ToList());

                if (!result.Success)
                {
                    StatusText.Text = $"Save failed: {result.ErrorMessage}";

                    System.Windows.MessageBox.Show(
                        result.ErrorMessage ?? "Save failed.",
                        "Save failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                StatusText.Text =
                    $"{result.Message} Backup: {result.BackupPath}";

                System.Windows.MessageBox.Show(
                    $"{result.Message}\n\nBackup created:\n{result.BackupPath}",
                    "Monster saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                int savedMonsterId = _selectedMonsterId.Value;

                ReloadWorkspaceAfterSave();

                MonsterListGrid.ItemsSource = null;
                ClearMonsterEditorOnly();

                LoadMonsterRowsIfNeeded();

                MonsterRow? savedRow = _filteredMonsterRows.FirstOrDefault(row => row.Id == savedMonsterId);

                if (savedRow is not null)
                {
                    MonsterListGrid.SelectedItem = savedRow;
                    LoadSelectedMonsterFields(savedMonsterId);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Save monster.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void ClearMonsterEditorOnly()
        {
            MonsterBasicsFieldGrid.ItemsSource = null;
            MonsterGearFieldGrid.ItemsSource = null;
            MonsterStatsFieldGrid.ItemsSource = null;
            MonsterMagicFieldGrid.ItemsSource = null;
            MonsterLootFieldGrid.ItemsSource = null;
            MonsterMiscFieldGrid.ItemsSource = null;

            _monsterFieldRows.Clear();
            _selectedMonsterId = null;

            SelectedMonsterText.Text = "No monster selected.";
            MonsterRawPreviewText.Text = "";
        }


        private void CommitMonsterButton_Click(object sender, RoutedEventArgs e)
        {
            SaveMonster();
        }

        private void EnsureMonsterFieldGridsConfigured()
        {
            ConfigureMonsterFieldGrid(MonsterBasicsFieldGrid);
            ConfigureMonsterFieldGrid(MonsterGearFieldGrid);
            ConfigureMonsterFieldGrid(MonsterStatsFieldGrid);
            ConfigureMonsterFieldGrid(MonsterMagicFieldGrid);
            ConfigureMonsterFieldGrid(MonsterLootFieldGrid);
            ConfigureMonsterFieldGrid(MonsterMiscFieldGrid);
        }

        private void ConfigureMonsterFieldGrid(DataGrid grid)
        {
            if (grid.Columns.Count > 0)
                return;

            grid.AutoGenerateColumns = false;
            grid.CanUserAddRows = false;
            grid.CanUserDeleteRows = false;
            grid.HeadersVisibility = DataGridHeadersVisibility.Column;
            grid.SelectionMode = DataGridSelectionMode.Single;

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Type",
                Binding = new System.Windows.Data.Binding("Type"),
                IsReadOnly = true,
                Width = new DataGridLength(80)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Key / Flag",
                Binding = new System.Windows.Data.Binding("Key"),
                IsReadOnly = true,
                Width = new DataGridLength(190)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Value",
                Binding = new System.Windows.Data.Binding("Value")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            if (ReferenceEquals(grid, MonsterBasicsFieldGrid))
            {
                FrameworkElementFactory CreateSpriteButtonFactory()
                {
                    var factory = new FrameworkElementFactory(typeof(Button));

                    factory.SetValue(Button.ContentProperty, "Pick");
                    factory.SetValue(Button.MinWidthProperty, 58.0);
                    factory.SetValue(Button.MarginProperty, new Thickness(2));

                    factory.SetBinding(Button.TagProperty, new System.Windows.Data.Binding("."));
                    factory.AddHandler(Button.ClickEvent, new RoutedEventHandler(PickMonsterSpriteButton_Click));
                    factory.AddHandler(Button.LoadedEvent, new RoutedEventHandler(MonsterSpritePickButton_Loaded));

                    return factory;
                }

                grid.Columns.Add(new DataGridTemplateColumn
                {
                    Header = "Sprite",
                    CellTemplate = new DataTemplate { VisualTree = CreateSpriteButtonFactory() },
                    Width = new DataGridLength(75)
                });
            }

            grid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Enabled / Flag",
                Binding = new System.Windows.Data.Binding("Enabled")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                },
                Width = new DataGridLength(110)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Known",
                Binding = new System.Windows.Data.Binding("KnownText"),
                IsReadOnly = true,
                Width = new DataGridLength(90)
            });
        }

        private void UpdateMonsterSpritePreview()
        {
            if (MonsterSpritePreviewStrip is null || MonsterSpritePreviewText is null)
                return;

            MonsterSpritePreviewStrip.Children.Clear();

            if (string.IsNullOrWhiteSpace(_serverFolderPath))
            {
                MonsterSpritePreviewText.Text = "Preview IDs: -";
                return;
            }

            string imageText = GetMonsterFieldValue("Image");
            string imageTypeText = GetMonsterFieldValue("ImageType");

            if (!int.TryParse(imageText, out int imageId))
            {
                MonsterSpritePreviewText.Text = "Preview IDs: -";
                return;
            }

            int imageType = 0;

            if (int.TryParse(imageTypeText, out int parsedImageType))
                imageType = parsedImageType;

            IReadOnlyList<int> previewIds = _itemSpriteService.CompositeIds(imageId, imageType);

            if (previewIds.Count == 0)
            {
                MonsterSpritePreviewText.Text = "Preview IDs: -";
                return;
            }

            Border container = new()
            {
                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 12, 0),
                ToolTip = $"ImageType {imageType}: {string.Join(", ", previewIds)}"
            };

            FrameworkElement previewLayout;

            if (imageType == 2 && previewIds.Count >= 4)
            {
                Grid grid = new();

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                AddMonsterCompositeGridSprite(grid, previewIds[0], 0, 0);
                AddMonsterCompositeGridSprite(grid, previewIds[1], 0, 1);
                AddMonsterCompositeGridSprite(grid, previewIds[2], 1, 0);
                AddMonsterCompositeGridSprite(grid, previewIds[3], 1, 1);

                previewLayout = grid;
            }
            else if (imageType == 1 && previewIds.Count >= 2)
            {
                StackPanel stack = new()
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                stack.Children.Add(CreateMonsterSpriteImage(previewIds[0]));
                stack.Children.Add(CreateMonsterSpriteImage(previewIds[1]));

                previewLayout = stack;
            }
            else
            {
                previewLayout = CreateMonsterSpriteImage(previewIds[0]);
            }

            container.Child = previewLayout;
            MonsterSpritePreviewStrip.Children.Add(container);

            MonsterSpritePreviewText.Text = $"Preview IDs: {string.Join(", ", previewIds)}";
        }

        private void AddMonsterCompositeGridSprite(Grid grid, int spriteId, int row, int column)
        {
            Image image = CreateMonsterSpriteImage(spriteId);

            Grid.SetRow(image, row);
            Grid.SetColumn(image, column);

            grid.Children.Add(image);
        }

        private Image CreateMonsterSpriteImage(int spriteId)
        {
            ImageSource? sprite = GetMonsterSpriteImage(spriteId);

            return new Image
            {
                Source = sprite,
                Width = 32,
                Height = 32,
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private ImageSource? GetMonsterSpriteImage(int spriteId)
        {
            if (string.IsNullOrWhiteSpace(_serverFolderPath))
                return null;

            string spritesFolder = Path.Combine(_serverFolderPath, "Sprites");

            if (!Directory.Exists(spritesFolder))
                return null;

            List<ItemSpriteTile> tiles = LoadSpriteTilesByPrefix(spritesFolder, "player");

            return tiles.FirstOrDefault(tile => tile.SpriteId == spriteId)?.Image;
        }

        private string GetMonsterFieldValue(string key)
        {
            MonsterFieldRow? row = _monsterFieldRows.FirstOrDefault(field =>
                field.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            return row?.Value?.Trim() ?? "";
        }

        private void CommitMonsterGrids()
        {
            Keyboard.ClearFocus();

            foreach (DataGrid grid in new[]
            {
                MonsterBasicsFieldGrid,
                MonsterGearFieldGrid,
                MonsterStatsFieldGrid,
                MonsterMagicFieldGrid,
                MonsterLootFieldGrid,
                MonsterMiscFieldGrid
            })
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }

        private void LoadMonsterFieldRowsIntoTabs(List<MonsterFieldRow> rows)
        {
            _monsterFieldRows = new ObservableCollection<MonsterFieldRow>(rows);

            MonsterBasicsFieldGrid.ItemsSource = _monsterFieldRows
                .Where(row => MonsterFieldGroup(row) == "Basics")
                .ToList();

            MonsterGearFieldGrid.ItemsSource = _monsterFieldRows
                .Where(row => MonsterFieldGroup(row) == "Gear")
                .ToList();

            MonsterStatsFieldGrid.ItemsSource = _monsterFieldRows
                .Where(row => MonsterFieldGroup(row) == "Stats")
                .ToList();

            MonsterMagicFieldGrid.ItemsSource = _monsterFieldRows
                .Where(row => MonsterFieldGroup(row) == "Magic")
                .ToList();

            LoadMonsterLootEntriesFromFields();

            MonsterMiscFieldGrid.ItemsSource = _monsterFieldRows
                .Where(row => MonsterFieldGroup(row) == "Misc")
                .ToList();

            UpdateMonsterSpritePreview();
        }

        private void LoadMonsterLootEntriesFromFields()
        {
            List<MonsterFieldRow> lootRows = _monsterFieldRows
                .Where(row => MonsterFieldGroup(row) == "Loot")
                .ToList();

            var entries = new List<MonsterLootEntryRow>();

            for (int index = 0; index < 10; index++)
            {
                string treasure = GetMonsterLootValue($"Treasure{index}");
                string qty = GetMonsterLootValue($"TreasureQty{index}");
                string chance = GetMonsterLootValue($"TreasureChance{index}");

                if (string.IsNullOrWhiteSpace(treasure) &&
                    string.IsNullOrWhiteSpace(qty) &&
                    string.IsNullOrWhiteSpace(chance))
                {
                    continue;
                }

                entries.Add(new MonsterLootEntryRow
                {
                    Index = index,
                    Treasure = treasure,
                    Qty = qty,
                    Chance = chance
                });
            }

            string deadItem = GetMonsterLootValue("DeadItem");
            string spawnItem = GetMonsterLootValue("SpawnItem");
            string spawnChance = GetMonsterLootValue("SpawnItemChance");

            if (!string.IsNullOrWhiteSpace(deadItem))
            {
                entries.Add(new MonsterLootEntryRow
                {
                    Index = 100,
                    Treasure = $"DeadItem={deadItem}",
                    Qty = "",
                    Chance = ""
                });
            }

            if (!string.IsNullOrWhiteSpace(spawnItem) || !string.IsNullOrWhiteSpace(spawnChance))
            {
                entries.Add(new MonsterLootEntryRow
                {
                    Index = 101,
                    Treasure = $"SpawnItem={spawnItem}",
                    Qty = "",
                    Chance = spawnChance
                });
            }

            _monsterLootEntryRows = new ObservableCollection<MonsterLootEntryRow>(entries);
            MonsterLootEntryGrid.ItemsSource = _monsterLootEntryRows;

            _selectedMonsterLootFieldRows.Clear();
            MonsterLootFieldGrid.ItemsSource = null;
            _selectedMonsterLootIndex = null;
            SelectedMonsterLootText.Text = "No loot entry selected.";
        }
        private void MonsterLootEntryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonsterLootEntryGrid.SelectedItem is not MonsterLootEntryRow entry)
                return;

            _selectedMonsterLootIndex = entry.Index;

            List<MonsterFieldRow> fields;

            if (entry.Index is >= 0 and <= 9)
            {
                fields = new List<MonsterFieldRow>
        {
            new() { Type = "Field", Key = $"Treasure{entry.Index}", Value = entry.Treasure, Enabled = !string.IsNullOrWhiteSpace(entry.Treasure), IsKnown = true },
            new() { Type = "Field", Key = $"TreasureQty{entry.Index}", Value = entry.Qty, Enabled = !string.IsNullOrWhiteSpace(entry.Qty), IsKnown = true },
            new() { Type = "Field", Key = $"TreasureChance{entry.Index}", Value = entry.Chance, Enabled = !string.IsNullOrWhiteSpace(entry.Chance), IsKnown = true },
        };
            }
            else
            {
                fields = new List<MonsterFieldRow>
        {
            new() { Type = "Field", Key = "DeadItem", Value = GetMonsterLootValue("DeadItem"), Enabled = !string.IsNullOrWhiteSpace(GetMonsterLootValue("DeadItem")), IsKnown = true },
            new() { Type = "Field", Key = "SpawnItem", Value = GetMonsterLootValue("SpawnItem"), Enabled = !string.IsNullOrWhiteSpace(GetMonsterLootValue("SpawnItem")), IsKnown = true },
            new() { Type = "Field", Key = "SpawnItemChance", Value = GetMonsterLootValue("SpawnItemChance"), Enabled = !string.IsNullOrWhiteSpace(GetMonsterLootValue("SpawnItemChance")), IsKnown = true },
            new() { Type = "Field", Key = "SpawnItemTimeout", Value = GetMonsterLootValue("SpawnItemTimeout"), Enabled = !string.IsNullOrWhiteSpace(GetMonsterLootValue("SpawnItemTimeout")), IsKnown = true },
        };
            }

            _selectedMonsterLootFieldRows = new ObservableCollection<MonsterFieldRow>(fields);
            MonsterLootFieldGrid.ItemsSource = _selectedMonsterLootFieldRows;

            SelectedMonsterLootText.Text = entry.Index is >= 0 and <= 9
                ? $"Loot Entry {entry.Index}"
                : "Special Loot Fields";
        }

        private void CommitMonsterLootButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMonsterLootIndex is null)
            {
                StatusText.Text = "Select a loot entry before committing.";
                return;
            }

            MonsterLootFieldGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            MonsterLootFieldGrid.CommitEdit(DataGridEditingUnit.Row, true);

            foreach (MonsterFieldRow editedRow in _selectedMonsterLootFieldRows)
            {
                MonsterFieldRow? target = _monsterFieldRows.FirstOrDefault(row =>
                    row.Key.Equals(editedRow.Key, StringComparison.OrdinalIgnoreCase));

                if (target is null)
                {
                    target = new MonsterFieldRow
                    {
                        Type = "Field",
                        Key = editedRow.Key,
                        Value = "",
                        Enabled = false,
                        IsKnown = true
                    };

                    _monsterFieldRows.Add(target);
                }

                target.Value = editedRow.Enabled ? editedRow.Value?.Trim() ?? "" : "";
                target.Enabled = editedRow.Enabled && !string.IsNullOrWhiteSpace(target.Value);
            }

            LoadMonsterLootEntriesFromFields();

            StatusText.Text = "Committed loot entry to monster fields. Use Commit / Save Monster to write monster.ini.";
        }
        private void NewMonsterLootButton_Click(object sender, RoutedEventArgs e)
        {
            int nextIndex = Enumerable.Range(0, 10)
                .FirstOrDefault(index => !_monsterLootEntryRows.Any(row => row.Index == index));

            if (_monsterLootEntryRows.Any(row => row.Index == nextIndex))
            {
                StatusText.Text = "No free Treasure0-9 slot is available.";
                return;
            }

            MonsterLootEntryRow entry = new()
            {
                Index = nextIndex,
                Treasure = "",
                Qty = "",
                Chance = ""
            };

            _monsterLootEntryRows.Add(entry);
            MonsterLootEntryGrid.Items.Refresh();
            MonsterLootEntryGrid.SelectedItem = entry;

            StatusText.Text = $"Created loot entry Treasure{nextIndex}.";
        }

        private void DeleteMonsterLootButton_Click(object sender, RoutedEventArgs e)
        {
            if (MonsterLootEntryGrid.SelectedItem is not MonsterLootEntryRow entry)
            {
                StatusText.Text = "Select a loot entry before deleting.";
                return;
            }

            if (entry.Index is < 0 or > 9)
            {
                StatusText.Text = "Special loot rows cannot be deleted here. Clear their values instead.";
                return;
            }

            foreach (string key in new[]
            {
                $"Treasure{entry.Index}",
                $"TreasureQty{entry.Index}",
                $"TreasureChance{entry.Index}"
            })
            {
                MonsterFieldRow? target = _monsterFieldRows.FirstOrDefault(row =>
                    row.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

                if (target is not null)
                {
                    target.Value = "";
                    target.Enabled = false;
                }
            }

            LoadMonsterLootEntriesFromFields();

            StatusText.Text = $"Deleted loot entry Treasure{entry.Index}. Use Commit / Save Monster to write monster.ini.";
        }
        private string GetMonsterLootValue(string key)
        {
            MonsterFieldRow? row = _monsterFieldRows.FirstOrDefault(field =>
                field.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            return row?.Value?.Trim() ?? "";
        }

        private static string MonsterFieldGroup(MonsterFieldRow row)
        {
            string key = row.Key.Trim();

            if (IsMonsterBasicsField(key))
                return "Basics";

            if (IsMonsterGearField(key))
                return "Gear";

            if (IsMonsterStatsField(key))
                return "Stats";

            if (IsMonsterMagicField(key))
                return "Magic";

            if (IsMonsterLootField(key))
                return "Loot";

            return "Misc";
        }

        private static bool IsMonsterBasicsField(string key)
        {
            return key.Equals("Name", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Category", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Catagory", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Level", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Image", StringComparison.OrdinalIgnoreCase)
                || key.Equals("ImageType", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Type", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMonsterGearField(string key)
        {
            return key.Equals("Weapon", StringComparison.OrdinalIgnoreCase)
                || key.Equals("RangeWeapon", StringComparison.OrdinalIgnoreCase)
                || key.Equals("ChestArmor", StringComparison.OrdinalIgnoreCase)
                || key.Equals("HeadArmor", StringComparison.OrdinalIgnoreCase)
                || key.Equals("LegArmor", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Shield", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Sheild", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMonsterStatsField(string key)
        {
            string[] exact =
            {
                "Life", "Health", "Stamina", "Mana",
                "MeleeDefense", "MissleDefense", "MagicDefense",
                "Scan", "Stealth", "Sneak", "Climb", "Swim", "Run",
                "Speed", "Roam", "KeepDistance", "FearFactor", "Fearfactor",
                "BashAL", "CutAL", "ThrustAL", "FireAL", "ColdAL", "ElectricAL", "MagicAL",
                "Dagger", "Sword", "Axe", "Mace", "Flail", "Staff", "Spear", "Scythe",
                "Bow", "Crossbow", "Throwing", "Pistol", "Rifle", "Hammer", "Ninja",
                "Halberd", "Unarmed", "Whip",
                "DamageLow", "DamageHigh", "DamageType", "AttackSpeed"
            };

            return exact.Any(value => key.Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsMonsterMagicField(string key)
        {
            return key.Equals("MagicPower", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("Cast", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMonsterLootField(string key)
        {
            return key.Equals("Treasure", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("Treasure", StringComparison.OrdinalIgnoreCase)
                || key.Equals("DeadItem", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Damagefragments", StringComparison.OrdinalIgnoreCase)
                || key.Equals("SpawnItem", StringComparison.OrdinalIgnoreCase)
                || key.Equals("SpawnItemChance", StringComparison.OrdinalIgnoreCase)
                || key.Equals("SpawnItemTimeout", StringComparison.OrdinalIgnoreCase);
        }

        private void LoadMonsterRowsIfNeeded()
        {
            if (MonsterListGrid.ItemsSource is not null)
                return;

            if (string.IsNullOrWhiteSpace(_monsterIniPath) ||
                !File.Exists(_monsterIniPath))
            {
                StatusText.Text = "monster.ini was not found.";
                return;
            }

            try
            {
                List<MonsterRow> rows = _monsterEditorService.LoadMonsterRows(_monsterIniPath);

                _monsterRows = new ObservableCollection<MonsterRow>(rows);
                _filteredMonsterRows = new ObservableCollection<MonsterRow>(rows);
                MonsterListGrid.ItemsSource = _filteredMonsterRows;
                ClearMonsterEditorOnly();

                StatusText.Text = $"Loaded monster.ini editor with {_monsterRows.Count} monster(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load monster.ini: {ex.Message}";

                MessageBox.Show(
                    ex.Message,
                    "Load monster.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void RefreshMonsterFilter()
        {
            string needle = MonsterSearchBox.Text.Trim();

            IEnumerable<MonsterRow> filtered = _monsterRows;

            if (!string.IsNullOrWhiteSpace(needle))
            {
                filtered = filtered.Where(monster =>
                    monster.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    monster.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            _filteredMonsterRows = new ObservableCollection<MonsterRow>(filtered);

            MonsterListGrid.ItemsSource = _filteredMonsterRows;
        }

        private void MonsterSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MonsterListGrid is null)
                return;

            RefreshMonsterFilter();
        }

        private void MonsterListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonsterListGrid.SelectedItem is not MonsterRow selectedMonster)
                return;

            LoadSelectedMonsterFields(selectedMonster.Id);
        }

        private void MonsterSpritePickButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is MonsterFieldRow row &&
                row.Key.Equals("Image", StringComparison.OrdinalIgnoreCase))
            {
                button.Visibility = Visibility.Visible;
                button.IsEnabled = true;
            }
            else
            {
                button.Visibility = Visibility.Collapsed;
                button.IsEnabled = false;
            }
        }

        private void OpenMonsterSpritePicker(MonsterFieldRow targetRow)
        {
            if (string.IsNullOrWhiteSpace(_serverFolderPath))
            {
                StatusText.Text = "Open a server folder before picking monster sprites.";
                return;
            }

            string spritesFolder = Path.Combine(_serverFolderPath, "Sprites");

            if (!Directory.Exists(spritesFolder))
            {
                StatusText.Text = $"Sprites folder not found: {spritesFolder}";

                MessageBox.Show(
                    $"Could not find the Sprites folder:\n\n{spritesFolder}",
                    "Sprites folder missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            List<ItemSpriteTile> tiles;

            try
            {
                tiles = LoadSpriteTilesByPrefix(spritesFolder, "player");
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load monster sprites: {ex.Message}";

                MessageBox.Show(
                    ex.Message,
                    "Sprite load failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (tiles.Count == 0)
            {
                StatusText.Text = "No player spritesheets were found.";

                MessageBox.Show(
                    $"No usable player sprite sheets were found in:\n\n{spritesFolder}\n\nExpected names like player0.bmp, player1.bmp, player2.png, etc.",
                    "No monster sprites found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var picker = new Window
            {
                Title = "Pick monster image sprite",
                Owner = this,
                Width = 1200,
                Height = 800,
                MinWidth = 900,
                MinHeight = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel
            {
                Margin = new Thickness(10)
            };

            var topPanel = new Grid
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchLabel = new TextBlock
            {
                Text = "Search ID:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var searchBox = new TextBox
            {
                MinWidth = 140,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var sheetLabel = new TextBlock
            {
                Text = "Sheet:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var sheetFilter = new ComboBox
            {
                Margin = new Thickness(0, 0, 12, 0)
            };

            var closeButton = new Button
            {
                Content = "Close",
                MinWidth = 90
            };

            Grid.SetColumn(searchLabel, 0);
            Grid.SetColumn(searchBox, 1);
            Grid.SetColumn(sheetLabel, 2);
            Grid.SetColumn(sheetFilter, 3);
            Grid.SetColumn(closeButton, 4);

            topPanel.Children.Add(searchLabel);
            topPanel.Children.Add(searchBox);
            topPanel.Children.Add(sheetLabel);
            topPanel.Children.Add(sheetFilter);
            topPanel.Children.Add(closeButton);

            DockPanel.SetDock(topPanel, Dock.Top);
            root.Children.Add(topPanel);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var wrap = new WrapPanel
            {
                Orientation = Orientation.Horizontal
            };

            scroll.Content = wrap;
            root.Children.Add(scroll);

            picker.Content = root;

            sheetFilter.Items.Add("All");

            foreach (string sheetName in tiles.Select(tile => tile.SheetName).Distinct().OrderBy(value => value))
                sheetFilter.Items.Add(sheetName);

            sheetFilter.SelectedItem = "All";

            void RenderTiles()
            {
                wrap.Children.Clear();

                string search = searchBox.Text.Trim();
                string selectedSheet = sheetFilter.SelectedItem?.ToString() ?? "All";

                IEnumerable<ItemSpriteTile> filtered = tiles;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(tile =>
                        tile.SpriteId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.Equals(selectedSheet, "All", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(tile =>
                        string.Equals(tile.SheetName, selectedSheet, StringComparison.OrdinalIgnoreCase));
                }

                List<ItemSpriteTile> visibleTiles = filtered.ToList();

                foreach (ItemSpriteTile tile in visibleTiles)
                {
                    var image = new Image
                    {
                        Source = tile.Image,
                        Width = 32,
                        Height = 32,
                        Stretch = Stretch.None,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var idText = new TextBlock
                    {
                        Text = tile.SpriteId.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 11
                    };

                    var stack = new StackPanel
                    {
                        Orientation = Orientation.Vertical
                    };

                    stack.Children.Add(image);
                    stack.Children.Add(idText);

                    var button = new Button
                    {
                        Content = stack,
                        Width = 62,
                        Height = 68,
                        Margin = new Thickness(3),
                        ToolTip = $"{tile.SheetName} | ID {tile.SpriteId}"
                    };

                    button.Click += (_, _) =>
                    {
                        targetRow.Value = tile.SpriteId.ToString();
                        targetRow.Enabled = true;

                        MonsterBasicsFieldGrid.Items.Refresh();
                        UpdateMonsterSpritePreview();

                        picker.Close();

                        StatusText.Text = $"Set monster Image to sprite ID {tile.SpriteId}.";
                    };

                    wrap.Children.Add(button);
                }

                statusText.Text = $"Showing {visibleTiles.Count} of {tiles.Count} sprite(s).";
            }

            searchBox.TextChanged += (_, _) => RenderTiles();
            sheetFilter.SelectionChanged += (_, _) => RenderTiles();
            closeButton.Click += (_, _) => picker.Close();

            RenderTiles();
            picker.ShowDialog();
        }
        private ObservableCollection<MonsterLootEntryRow> _monsterLootEntryRows = new();
        private ObservableCollection<MonsterFieldRow> _selectedMonsterLootFieldRows = new();
        private int? _selectedMonsterLootIndex;

        private sealed class MonsterLootEntryRow
        {
            public int Index { get; set; }

            public string Treasure { get; set; } = "";

            public string Qty { get; set; } = "";

            public string Chance { get; set; } = "";
        }
        private void MonsterFieldGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(UpdateMonsterSpritePreview));
        }
        private static List<ItemSpriteTile> LoadSpriteTilesByPrefix(string spritesFolder, string sheetPrefix)
        {
            var results = new List<ItemSpriteTile>();

            string[] supportedExtensions =
            {
        ".bmp",
        ".png",
        ".gif",
        ".jpg",
        ".jpeg"
    };

            foreach (string path in Directory.EnumerateFiles(spritesFolder))
            {
                string extension = Path.GetExtension(path);

                if (!supportedExtensions.Any(ext =>
                        string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string stem = Path.GetFileNameWithoutExtension(path);

                if (!stem.StartsWith(sheetPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string sheetNumberText = stem[sheetPrefix.Length..];

                if (!int.TryParse(sheetNumberText, out int sheetNumber))
                    continue;

                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                int columns = bitmap.PixelWidth / 32;
                int rows = bitmap.PixelHeight / 32;

                if (columns <= 0 || rows <= 0)
                    continue;

                int firstSpriteId = sheetNumber * 100;
                string sheetName = Path.GetFileName(path);

                for (int index = 0; index < columns * rows; index++)
                {
                    int column = index % columns;
                    int row = index / columns;
                    int spriteId = firstSpriteId + index;

                    var crop = new CroppedBitmap(
                        bitmap,
                        new Int32Rect(column * 32, row * 32, 32, 32));

                    crop.Freeze();

                    results.Add(new ItemSpriteTile
                    {
                        SpriteId = spriteId,
                        SheetName = sheetName,
                        Image = crop
                    });
                }
            }

            return results
                .OrderBy(tile => tile.SpriteId)
                .ToList();
        }

        private void PickMonsterSpriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.Tag is not MonsterFieldRow targetRow)
            {
                StatusText.Text = "Could not determine which monster image row to edit.";
                return;
            }

            if (!targetRow.Key.Equals("Image", StringComparison.OrdinalIgnoreCase))
            {
                StatusText.Text = "Only the Image field can use the monster sprite picker.";
                return;
            }

            OpenMonsterSpritePicker(targetRow);
        }

        private void LoadSelectedMonsterFields(int monsterId)
        {
            if (string.IsNullOrWhiteSpace(_monsterIniPath) ||
                !File.Exists(_monsterIniPath))
            {
                StatusText.Text = "monster.ini was not found.";
                return;
            }

            try
            {
                List<MonsterFieldRow> rows = _monsterEditorService.LoadFieldRows(
                    _monsterIniPath,
                    monsterId);

                _selectedMonsterId = monsterId;
                LoadMonsterFieldRowsIntoTabs(rows);

                MonsterRawPreviewText.Text = _previewService.GetMonsterPreview(
                    _monsterIniPath,
                    monsterId);

                MonsterRow? monster = _monsterRows.FirstOrDefault(row => row.Id == monsterId);

                SelectedMonsterText.Text = monster is null
                    ? $"Monster {monsterId}"
                    : $"Monster {monster.Id}: {monster.Name}";

                SaveButton.IsEnabled = true;

                StatusText.Text = $"Loaded Monster={monsterId} with {_monsterFieldRows.Count} editable row(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load Monster={monsterId}: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load monster failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }



        #endregion

        #region Item
        private void ShowItemView()
        {
            EnsureItemFieldGridsConfigured();
            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Collapsed;
            TreasureView.Visibility = Visibility.Collapsed;
            MagicView.Visibility = Visibility.Collapsed;
            MonsterView.Visibility = Visibility.Collapsed;
            ItemView.Visibility = Visibility.Visible;
            UsageView.Visibility = Visibility.Collapsed;
            MultiUseView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;

            ExportCsvButton.IsEnabled = _itemIniPath is not null && File.Exists(_itemIniPath);
            LoadItemRowsIfNeeded();
            SaveButton.IsEnabled = ItemBaseFieldGrid.ItemsSource is not null;
            StatusText.Text = "Item editor loaded.";
        }

        private void CommitItemButton_Click(object sender, RoutedEventArgs e)
        {
            SaveItem();
        }

        private void PickItemAnimation0Button_Click(object sender, RoutedEventArgs e)
        {
            OpenItemAnimationPicker("Animation0");
        }

        private void PickItemAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.Tag is not string targetField ||
                string.IsNullOrWhiteSpace(targetField))
            {
                StatusText.Text = "Could not determine which animation field to edit.";
                return;
            }

            targetField = targetField.Trim();

            if (!IsItemAnimationField(targetField))
            {
                StatusText.Text = $"{targetField} is not a sprite animation field.";
                return;
            }

            OpenItemAnimationPicker(targetField);
        }
        private static readonly string[] ArmorSpotOptions =
        {
            "",
            "Head",
            "Chest",
            "Legs",
            "Back"
        };
        private void OpenItemAnimationPicker(string targetField)
        {
            if (!IsItemAnimationField(targetField))
            {
                StatusText.Text = $"{targetField} is not a valid item animation field.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_serverFolderPath))
            {
                StatusText.Text = "Open a server folder before picking sprites.";
                return;
            }

            string spritesFolder = Path.Combine(_serverFolderPath, "Sprites");

            if (!Directory.Exists(spritesFolder))
            {
                StatusText.Text = $"Sprites folder not found: {spritesFolder}";
                MessageBox.Show(
                    $"Could not find the Sprites folder:\n\n{spritesFolder}",
                    "Sprites folder missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            List<ItemSpriteTile> tiles;

            try
            {
                tiles = LoadItemSpriteTiles(spritesFolder);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load item sprites: {ex.Message}";
                MessageBox.Show(
                    ex.Message,
                    "Sprite load failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (tiles.Count == 0)
            {
                StatusText.Text = "No item spritesheets were found.";
                MessageBox.Show(
                    $"No usable item sprite sheets were found in:\n\n{spritesFolder}\n\nExpected names like item0.bmp, item1.bmp, item2.png, etc.",
                    "No item sprites found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var picker = new Window
            {
                Title = $"Pick sprite for {targetField}",
                Owner = this,
                Width = 1200,
                Height = 800,
                MinWidth = 900,
                MinHeight = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel
            {
                Margin = new Thickness(10)
            };

            var topPanel = new Grid
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchLabel = new TextBlock
            {
                Text = "Search ID:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var searchBox = new TextBox
            {
                MinWidth = 140,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var sheetLabel = new TextBlock
            {
                Text = "Sheet:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var sheetFilter = new ComboBox
            {
                Margin = new Thickness(0, 0, 12, 0)
            };

            var closeButton = new Button
            {
                Content = "Close",
                MinWidth = 90
            };

            Grid.SetColumn(searchLabel, 0);
            Grid.SetColumn(searchBox, 1);
            Grid.SetColumn(sheetLabel, 2);
            Grid.SetColumn(sheetFilter, 3);
            Grid.SetColumn(closeButton, 4);

            topPanel.Children.Add(searchLabel);
            topPanel.Children.Add(searchBox);
            topPanel.Children.Add(sheetLabel);
            topPanel.Children.Add(sheetFilter);
            topPanel.Children.Add(closeButton);

            DockPanel.SetDock(topPanel, Dock.Top);
            root.Children.Add(topPanel);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var wrap = new WrapPanel
            {
                Orientation = Orientation.Horizontal
            };

            scroll.Content = wrap;
            root.Children.Add(scroll);

            picker.Content = root;

            sheetFilter.Items.Add("All");

            foreach (string sheetName in tiles.Select(tile => tile.SheetName).Distinct().OrderBy(value => value))
                sheetFilter.Items.Add(sheetName);

            sheetFilter.SelectedItem = "All";

            void RenderTiles()
            {
                wrap.Children.Clear();

                string search = searchBox.Text.Trim();
                string selectedSheet = sheetFilter.SelectedItem?.ToString() ?? "All";

                IEnumerable<ItemSpriteTile> filtered = tiles;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(tile =>
                        tile.SpriteId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.Equals(selectedSheet, "All", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(tile =>
                        string.Equals(tile.SheetName, selectedSheet, StringComparison.OrdinalIgnoreCase));
                }

                List<ItemSpriteTile> visibleTiles = filtered.ToList();

                foreach (ItemSpriteTile tile in visibleTiles)
                {
                    var image = new Image
                    {
                        Source = tile.Image,
                        Width = 32,
                        Height = 32,
                        Stretch = Stretch.None,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var idText = new TextBlock
                    {
                        Text = tile.SpriteId.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 11
                    };

                    var stack = new StackPanel
                    {
                        Orientation = Orientation.Vertical
                    };

                    stack.Children.Add(image);
                    stack.Children.Add(idText);

                    var button = new Button
                    {
                        Content = stack,
                        Width = 62,
                        Height = 68,
                        Margin = new Thickness(3),
                        ToolTip = $"{tile.SheetName} | ID {tile.SpriteId}"
                    };

                    button.Click += (_, _) =>
                    {
                        SetItemFieldValue(targetField, tile.SpriteId.ToString(), enableField: true);
                        LoadItemFieldRowsIntoTabs(_itemFieldRows.ToList());
                        UpdateItemSpritePreview();
                        picker.Close();

                        StatusText.Text = $"Set {targetField} to sprite ID {tile.SpriteId}.";
                    };

                    wrap.Children.Add(button);
                }

                statusText.Text = $"Showing {visibleTiles.Count} of {tiles.Count} sprite(s).";
            }

            searchBox.TextChanged += (_, _) => RenderTiles();
            sheetFilter.SelectionChanged += (_, _) => RenderTiles();
            closeButton.Click += (_, _) => picker.Close();

            RenderTiles();
            picker.ShowDialog();
        }

        private static bool IsItemAnimationField(string fieldName)
        {
            if (!fieldName.StartsWith("Animation", StringComparison.OrdinalIgnoreCase))
                return false;

            string numberText = fieldName["Animation".Length..];

            return int.TryParse(numberText, out int index) &&
                   index >= 0 &&
                   index <= 9;
        }

        private void SetItemFieldValue(string label, string value, bool enableField = true)
        {
            ItemFieldRow? row = _itemFieldRows.FirstOrDefault(field =>
                string.Equals(field.Label, label, StringComparison.OrdinalIgnoreCase));

            if (row is null)
            {
                StatusText.Text = $"Could not find item field: {label}";
                return;
            }

            row.Value = value;

            if (enableField)
                row.Enabled = true;
        }

        private void ItemSpritePreviewModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (ItemSpritePreviewStrip is null || ItemSpritePreviewText is null)
                return;

            UpdateItemSpritePreview();
        }
        private static List<ItemSpriteTile> LoadItemSpriteTiles(string spritesFolder)
        {
            var results = new List<ItemSpriteTile>();

            string[] supportedExtensions =
            {
                ".bmp",
                ".png",
                ".gif",
                ".jpg",
                ".jpeg"
            };

            foreach (string path in Directory.EnumerateFiles(spritesFolder))
            {
                string extension = Path.GetExtension(path);

                if (!supportedExtensions.Any(ext =>
                        string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string stem = Path.GetFileNameWithoutExtension(path);

                if (!stem.StartsWith("item", StringComparison.OrdinalIgnoreCase))
                    continue;

                string sheetNumberText = stem["item".Length..];

                if (!int.TryParse(sheetNumberText, out int sheetNumber))
                    continue;

                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                int columns = bitmap.PixelWidth / 32;
                int rows = bitmap.PixelHeight / 32;

                if (columns <= 0 || rows <= 0)
                    continue;

                int firstSpriteId = sheetNumber * 100;
                string sheetName = Path.GetFileName(path);

                for (int index = 0; index < columns * rows; index++)
                {
                    int column = index % columns;
                    int row = index / columns;
                    int spriteId = firstSpriteId + index;

                    var crop = new CroppedBitmap(
                        bitmap,
                        new Int32Rect(column * 32, row * 32, 32, 32));

                    crop.Freeze();

                    results.Add(new ItemSpriteTile
                    {
                        SpriteId = spriteId,
                        SheetName = sheetName,
                        Image = crop
                    });
                }
            }

            return results
                .OrderBy(tile => tile.SpriteId)
                .ToList();
        }
        private void UpdateItemSpritePreview()
        {
            if (ItemSpritePreviewStrip is null || ItemSpritePreviewText is null)
                return;

            ItemSpritePreviewStrip.Children.Clear();

            if (string.IsNullOrWhiteSpace(_serverFolderPath))
            {
                ItemSpritePreviewText.Text = "Preview IDs: -";
                return;
            }

            string mode = GetItemSpritePreviewMode();

            var previewParts = new List<string>();

            if (mode == "Composite" || mode == "Combined")
            {
                AddCompositeItemSpritePreview(previewParts);
            }

            if (mode == "Animation Fields" || mode == "Combined")
            {
                AddAnimationFieldSpritePreviews(previewParts);
            }

            ItemSpritePreviewText.Text = previewParts.Count == 0
                ? "Preview IDs: -"
                : $"Preview IDs: {string.Join(", ", previewParts.Select(part => part.Split(':').Last().Trim()))}";
        }
        private string GetItemSpritePreviewMode()
        {
            if (ItemSpritePreviewModeCombo?.SelectedItem is ComboBoxItem item &&
                item.Content is string text &&
                !string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }

            return "Composite";
        }

        private void AddCompositeItemSpritePreview(List<string> previewParts)
        {
            string animation0Text = GetItemFieldValue("Animation0");
            string imageTypeText = GetItemFieldValue("ImageType");

            if (!int.TryParse(animation0Text, out int animation0))
                return;

            int imageType = 0;

            if (int.TryParse(imageTypeText, out int parsedImageType))
                imageType = parsedImageType;

            IReadOnlyList<int> compositeIds = _itemSpriteService.CompositeIds(animation0, imageType);

            if (compositeIds.Count == 0)
                return;

            var compositeContainer = new Border
            {
                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 12, 0),
                ToolTip = $"Composite Type {imageType}: {string.Join(", ", compositeIds)}"
            };

            FrameworkElement spriteLayout;

            if (imageType == 2 && compositeIds.Count >= 4)
            {
                var grid = new Grid();

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                AddCompositeGridSprite(grid, compositeIds[0], 0, 0);
                AddCompositeGridSprite(grid, compositeIds[1], 0, 1);
                AddCompositeGridSprite(grid, compositeIds[2], 1, 0);
                AddCompositeGridSprite(grid, compositeIds[3], 1, 1);

                spriteLayout = grid;
            }
            else if (imageType == 1 && compositeIds.Count >= 2)
            {
                var stack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                stack.Children.Add(CreateSpriteImage(compositeIds[0]));
                stack.Children.Add(CreateSpriteImage(compositeIds[1]));

                spriteLayout = stack;
            }
            else
            {
                spriteLayout = CreateSpriteImage(compositeIds[0]);
            }

            compositeContainer.Child = spriteLayout;
            ItemSpritePreviewStrip.Children.Add(compositeContainer);

            previewParts.AddRange(compositeIds.Select(id => id.ToString()));
        }
        private void AddCompositeGridSprite(Grid grid, int spriteId, int row, int column)
        {
            Image image = CreateSpriteImage(spriteId);

            Grid.SetRow(image, row);
            Grid.SetColumn(image, column);

            grid.Children.Add(image);
        }

        private void AddAnimationFieldSpritePreviews(List<string> previewParts)
        {
            List<ItemFieldRow> activeAnimationRows = _itemFieldRows
                .Where(row => IsItemAnimationField(row.Label))
                .OrderBy(row => AnimationFieldIndex(row.Label))
                .Where(row => IsItemAnimationPreviewActive(row))
                .ToList();

            foreach (ItemFieldRow row in activeAnimationRows)
            {
                if (!int.TryParse(row.Value?.Trim(), out int spriteId))
                    continue;

                var border = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4),
                    Margin = new Thickness(0, 0, 8, 0),
                    ToolTip = $"{row.Label}: {spriteId}"
                };

                border.Child = CreateSpriteImage(spriteId);
                ItemSpritePreviewStrip.Children.Add(border);

                previewParts.Add(spriteId.ToString());
            }
        }
        private Image CreateSpriteImage(int spriteId)
        {
            ImageSource? sprite = _itemSpriteService.GetItemSpriteImage(_serverFolderPath, spriteId.ToString());

            return new Image
            {
                Source = sprite,
                Width = 32,
                Height = 32,
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        private static bool IsItemAnimationPreviewActive(ItemFieldRow row)
        {
            string value = row.Value?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(value))
                return false;

            return int.TryParse(value, out _);
        }
        private static readonly HashSet<string> ItemSkillReferenceFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "CombatSkill",
            "SkillIDBonus",
            "SkillBonusID",
            "StarterSkill"
        };

        private static readonly HashSet<string> ItemItemReferenceFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "DefaultGrowthDeadItem",
            "GrowthDeadItem",
            "DegradeItem",
            "GrowthItem"
        };

        private static readonly HashSet<string> ItemMonsterReferenceFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "SpawnMonster"
        };

        private static bool IsItemSkillReferenceField(string fieldName)
        {
            return ItemSkillReferenceFields.Contains(fieldName);
        }

        private static bool IsItemItemReferenceField(string fieldName)
        {
            return ItemItemReferenceFields.Contains(fieldName);
        }

        private static bool IsItemMonsterReferenceField(string fieldName)
        {
            return ItemMonsterReferenceFields.Contains(fieldName);
        }

        private static bool IsItemReferencePickerField(string fieldName)
        {
            return IsItemSkillReferenceField(fieldName)
                || IsItemItemReferenceField(fieldName)
                || IsItemMonsterReferenceField(fieldName);
        }
        private static int AnimationFieldIndex(string fieldName)
        {
            if (!fieldName.StartsWith("Animation", StringComparison.OrdinalIgnoreCase))
                return int.MaxValue;

            string numberText = fieldName["Animation".Length..];

            return int.TryParse(numberText, out int index)
                ? index
                : int.MaxValue;
        }
        private string GetItemFieldValue(string label)
        {
            ItemFieldRow? row = _itemFieldRows.FirstOrDefault(field =>
                string.Equals(field.Label, label, StringComparison.OrdinalIgnoreCase));

            return row?.Value?.Trim() ?? "";
        }
        private sealed class ItemSpriteTile
        {
            public int SpriteId { get; init; }

            public string SheetName { get; init; } = "";

            public BitmapSource Image { get; init; } = null!;
        }

        private void LoadItemRowsIfNeeded()
        {
            if (ItemListGrid.ItemsSource is not null)
                return;

            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
            {
                StatusText.Text = "item.ini was not found.";
                return;
            }

            try
            {
                List<ItemRow> rows = _itemEditorService.LoadItemRows(_itemIniPath);
                _itemRows = new ObservableCollection<ItemRow>(rows);
                _filteredItemRows = new ObservableCollection<ItemRow>(rows);
                ItemListGrid.ItemsSource = _filteredItemRows;
                ClearItemEditorOnly();
                StatusText.Text = $"Loaded item.ini editor with {_itemRows.Count} item(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load item.ini: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Load item.ini failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ItemSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshItemFilter();
        }

        private void RefreshItemFilter()
        {
            string needle = ItemSearchBox.Text.Trim();
            IEnumerable<ItemRow> filtered = _itemRows;

            if (!string.IsNullOrWhiteSpace(needle))
            {
                filtered = filtered.Where(item =>
                    item.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.Class.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.SubType.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.Animation0.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.Size.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.Burden.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.Value.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            _filteredItemRows = new ObservableCollection<ItemRow>(filtered);
            ItemListGrid.ItemsSource = _filteredItemRows;
        }

        private void ItemListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemListGrid.SelectedItem is not ItemRow selectedItem)
                return;

            LoadSelectedItemFields(selectedItem.Id);
        }

        private void LoadSelectedItemFields(int itemId)
        {
            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
            {
                StatusText.Text = "item.ini was not found.";
                return;
            }

            try
            {
                List<ItemFieldRow> rows = _itemEditorService.LoadFieldRows(_itemIniPath, itemId);
                LoadItemFieldRowsIntoTabs(rows);
                _selectedItemId = itemId;

                ItemRawPreviewText.Text = _itemEditorService.GetItemPreview(_itemIniPath, itemId);
                ItemRow? item = _itemRows.FirstOrDefault(row => row.Id == itemId);
                SelectedItemText.Text = item is null ? $"Item {itemId}" : $"Item {item.Id}: {item.Name}";
                SaveButton.IsEnabled = true;
                StatusText.Text = $"Loaded Item={itemId}.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load Item={itemId}: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Load item failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ConfigureItemFieldGrid(DataGrid grid)
        {
            if (grid.Columns.Count > 0)
                return;

            grid.AutoGenerateColumns = false;
            grid.CanUserAddRows = false;
            grid.CanUserDeleteRows = false;
            grid.HeadersVisibility = DataGridHeadersVisibility.Column;
            grid.SelectionMode = DataGridSelectionMode.Single;

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Field",
                Binding = new System.Windows.Data.Binding("Label"),
                IsReadOnly = true,
                Width = new DataGridLength(180)
            });

            FrameworkElementFactory CreateValueComboFactory()
            {
                var factory = new FrameworkElementFactory(typeof(ComboBox));

                factory.SetBinding(ComboBox.TextProperty, new System.Windows.Data.Binding("Value")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                });

                factory.SetBinding(ItemsControl.ItemsSourceProperty, new System.Windows.Data.Binding("Options"));
                factory.SetValue(ComboBox.IsEditableProperty, true);

                return factory;
            }

            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Value",
                CellTemplate = new DataTemplate { VisualTree = CreateValueComboFactory() },
                CellEditingTemplate = new DataTemplate { VisualTree = CreateValueComboFactory() },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            if (ReferenceEquals(grid, ItemAnimationsFieldGrid))
            {
                FrameworkElementFactory CreatePickButtonFactory()
                {
                    var factory = new FrameworkElementFactory(typeof(Button));

                    factory.SetValue(Button.ContentProperty, "Pick");
                    factory.SetValue(Button.MinWidthProperty, 58.0);
                    factory.SetValue(Button.MarginProperty, new Thickness(2));

                    factory.SetBinding(Button.TagProperty, new System.Windows.Data.Binding("Label"));
                    factory.AddHandler(Button.ClickEvent, new RoutedEventHandler(PickItemAnimationButton_Click));
                    factory.AddHandler(Button.LoadedEvent, new RoutedEventHandler(ItemAnimationPickButton_Loaded));

                    return factory;
                }

                grid.Columns.Add(new DataGridTemplateColumn
                {
                    Header = "Sprite",
                    CellTemplate = new DataTemplate { VisualTree = CreatePickButtonFactory() },
                    Width = new DataGridLength(75)
                });
            }
            if (ReferenceEquals(grid, ItemEquipmentFieldGrid)
                || ReferenceEquals(grid, ItemGrowthFieldGrid)
                || ReferenceEquals(grid, ItemBonusFieldGrid)
                || ReferenceEquals(grid, ItemSkillFieldGrid)
                || ReferenceEquals(grid, ItemMobsFieldGrid))
            {
                FrameworkElementFactory CreateReferencePickButtonFactory()
                {
                    var factory = new FrameworkElementFactory(typeof(Button));

                    factory.SetValue(Button.ContentProperty, "Pick");
                    factory.SetValue(Button.MinWidthProperty, 58.0);
                    factory.SetValue(Button.MarginProperty, new Thickness(2));

                    factory.SetBinding(Button.TagProperty, new System.Windows.Data.Binding("Label"));
                    factory.AddHandler(Button.ClickEvent, new RoutedEventHandler(PickItemReferenceButton_Click));
                    factory.AddHandler(Button.LoadedEvent, new RoutedEventHandler(ItemReferencePickButton_Loaded));

                    return factory;
                }

                grid.Columns.Add(new DataGridTemplateColumn
                {
                    Header = "Pick",
                    CellTemplate = new DataTemplate { VisualTree = CreateReferencePickButtonFactory() },
                    Width = new DataGridLength(75)
                });
            }
            grid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Enabled / Flag",
                Binding = new System.Windows.Data.Binding("Enabled")
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                },
                Width = new DataGridLength(110)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "INI Key",
                Binding = new System.Windows.Data.Binding("IniKey"),
                IsReadOnly = true,
                Width = new DataGridLength(140)
            });
        }
        private void OpenItemSkillReferencePicker(string targetField)
        {
            if (string.IsNullOrWhiteSpace(_skillIniPath) || !File.Exists(_skillIniPath))
            {
                StatusText.Text = "skill.ini was not found.";
                MessageBox.Show("skill.ini was not found.", "Skill picker", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<SkillRow> skills;

            try
            {
                skills = _skillEditorService.LoadSkillRows(_skillIniPath)
                    .OrderBy(skill => skill.Name)
                    .ThenBy(skill => skill.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load skills: {ex.Message}";
                MessageBox.Show(ex.Message, "Skill picker failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var picker = new Window
            {
                Title = $"Pick skill for {targetField}",
                Owner = this,
                Width = 700,
                Height = 600,
                MinWidth = 520,
                MinHeight = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel { Margin = new Thickness(10) };

            var searchBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("Id"),
                Width = new DataGridLength(80)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new System.Windows.Data.Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<SkillRow> filtered = skills;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(skill =>
                        skill.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        skill.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<SkillRow> visible = filtered.ToList();
                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {skills.Count} skill(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not SkillRow skill)
                    return;

                SetItemFieldValue(targetField, skill.Name, enableField: true);
                LoadItemFieldRowsIntoTabs(_itemFieldRows.ToList());
                picker.Close();

                StatusText.Text = $"Set {targetField} to {skill.Name}.";
            }

            searchBox.TextChanged += (_, _) => Refresh();
            grid.MouseDoubleClick += (_, _) => SelectCurrent();
            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }
        private void OpenItemItemReferencePicker(string targetField)
        {
            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
            {
                StatusText.Text = "item.ini was not found.";
                MessageBox.Show("item.ini was not found.", "Item picker", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<ItemRow> items;

            try
            {
                items = _itemEditorService.LoadItemRows(_itemIniPath)
                    .OrderBy(item => item.Name)
                    .ThenBy(item => item.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load items: {ex.Message}";
                MessageBox.Show(ex.Message, "Item picker failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var picker = new Window
            {
                Title = $"Pick item for {targetField}",
                Owner = this,
                Width = 850,
                Height = 650,
                MinWidth = 620,
                MinHeight = 460,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel { Margin = new Thickness(10) };

            var searchBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("Id"),
                Width = new DataGridLength(80)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new System.Windows.Data.Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Class",
                Binding = new System.Windows.Data.Binding("Class"),
                Width = new DataGridLength(120)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "SubType",
                Binding = new System.Windows.Data.Binding("SubType"),
                Width = new DataGridLength(140)
            });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<ItemRow> filtered = items;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(item =>
                        item.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.Class.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        item.SubType.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<ItemRow> visible = filtered.ToList();
                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {items.Count} item(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not ItemRow item)
                    return;

                SetItemFieldValue(targetField, item.Id.ToString(), enableField: true);
                LoadItemFieldRowsIntoTabs(_itemFieldRows.ToList());
                picker.Close();

                StatusText.Text = $"Set {targetField} to Item={item.Id} ({item.Name}).";
            }

            searchBox.TextChanged += (_, _) => Refresh();
            grid.MouseDoubleClick += (_, _) => SelectCurrent();
            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }
        private void PickItemReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.Tag is not string targetField ||
                string.IsNullOrWhiteSpace(targetField))
            {
                StatusText.Text = "Could not determine which field to edit.";
                return;
            }

            targetField = targetField.Trim();

            if (IsItemSkillReferenceField(targetField))
            {
                OpenItemSkillReferencePicker(targetField);
                return;
            }

            if (IsItemItemReferenceField(targetField))
            {
                OpenItemItemReferencePicker(targetField);
                return;
            }

            if (IsItemMonsterReferenceField(targetField))
            {
                OpenItemMonsterReferencePicker(targetField);
                return;
            }

            StatusText.Text = $"{targetField} does not have a picker.";
        }
        private void OpenItemMonsterReferencePicker(string targetField)
        {
            if (string.IsNullOrWhiteSpace(_monsterIniPath) || !File.Exists(_monsterIniPath))
            {
                StatusText.Text = "monster.ini was not found.";
                MessageBox.Show("monster.ini was not found.", "Monster picker", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<MonsterRow> monsters;

            try
            {
                monsters = _monsterEditorService.LoadMonsterRows(_monsterIniPath)
                    .OrderBy(monster => monster.Name)
                    .ThenBy(monster => monster.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load monsters: {ex.Message}";
                MessageBox.Show(ex.Message, "Monster picker failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var picker = new Window
            {
                Title = $"Pick monster for {targetField}",
                Owner = this,
                Width = 760,
                Height = 620,
                MinWidth = 560,
                MinHeight = 440,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new DockPanel { Margin = new Thickness(10) };

            var searchBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            DockPanel.SetDock(searchBox, Dock.Top);
            root.Children.Add(searchBox);

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            DockPanel.SetDock(statusText, Dock.Bottom);
            root.Children.Add(statusText);

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single
            };

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("Id"),
                Width = new DataGridLength(80)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new System.Windows.Data.Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Fields",
                Binding = new System.Windows.Data.Binding("FieldCount"),
                Width = new DataGridLength(80)
            });

            root.Children.Add(grid);
            picker.Content = root;

            void Refresh()
            {
                string needle = searchBox.Text.Trim();

                IEnumerable<MonsterRow> filtered = monsters;

                if (!string.IsNullOrWhiteSpace(needle))
                {
                    filtered = filtered.Where(monster =>
                        monster.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        monster.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                List<MonsterRow> visible = filtered.ToList();
                grid.ItemsSource = visible;
                statusText.Text = $"Showing {visible.Count} of {monsters.Count} monster(s).";
            }

            void SelectCurrent()
            {
                if (grid.SelectedItem is not MonsterRow monster)
                    return;

                SetItemFieldValue(targetField, monster.Id.ToString(), enableField: true);
                LoadItemFieldRowsIntoTabs(_itemFieldRows.ToList());
                picker.Close();

                StatusText.Text = $"Set {targetField} to Monster={monster.Id} ({monster.Name}).";
            }

            searchBox.TextChanged += (_, _) => Refresh();

            grid.MouseDoubleClick += (_, _) => SelectCurrent();

            grid.KeyDown += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Enter)
                {
                    SelectCurrent();
                    keyArgs.Handled = true;
                }
            };

            Refresh();
            picker.ShowDialog();
        }
        private void ItemReferencePickButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is ItemFieldRow row && IsItemReferencePickerField(row.Label))
            {
                button.Visibility = Visibility.Visible;
                button.IsEnabled = true;
            }
            else
            {
                button.Visibility = Visibility.Collapsed;
                button.IsEnabled = false;
            }
        }
        private void EnsureItemFieldGridsConfigured()
        {
            DataGrid[] grids =
            {
                ItemBaseFieldGrid, ItemGeneralFieldGrid, ItemEquipmentFieldGrid, ItemAnimationsFieldGrid,
                ItemArmorFieldGrid, ItemWeaponFieldGrid, ItemGrowthFieldGrid, ItemFoodFieldGrid,
                ItemBonusFieldGrid, ItemSkillFieldGrid, ItemTraderFieldGrid, ItemMobsFieldGrid,
                ItemProximityFieldGrid, ItemMiscFieldGrid
            };

            foreach (DataGrid grid in grids)
                ConfigureItemFieldGrid(grid);
        }
        private void ItemAnimationPickButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is ItemFieldRow row && IsItemAnimationField(row.Label))
            {
                button.Visibility = Visibility.Visible;
                button.IsEnabled = true;
            }
            else
            {
                button.Visibility = Visibility.Collapsed;
                button.IsEnabled = false;
            }
        }
        private void LoadItemFieldRowsIntoTabs(List<ItemFieldRow> rows)
        {
            _itemFieldRows = new ObservableCollection<ItemFieldRow>(rows);

            ItemBaseFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Base").ToList();
            ItemGeneralFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "General").ToList();
            ItemEquipmentFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Equipment").ToList();
            ItemAnimationsFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Animations").ToList();
            ItemArmorFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Armor").ToList();
            ItemWeaponFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Weapon").ToList();
            ItemGrowthFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Growth").ToList();
            ItemFoodFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Food").ToList();
            ItemBonusFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Bonus").ToList();
            ItemSkillFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Skill").ToList();
            ItemTraderFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Trader").ToList();
            ItemMobsFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Mobs").ToList();
            ItemProximityFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Proximity").ToList();
            ItemMiscFieldGrid.ItemsSource = _itemFieldRows.Where(row => row.Group == "Misc").ToList();
            UpdateItemSpritePreview();
        }

        private void NewItemButton_Click(object sender, RoutedEventArgs e)
        {
            int nextId = _itemRows.Any() ? _itemRows.Max(row => row.Id) + 1 : 1;
            LoadItemFieldRowsIntoTabs(_itemEditorService.CreateBlankFieldRows(nextId));
            _selectedItemId = null;
            SelectedItemText.Text = $"New Item: {nextId}";
            ItemRawPreviewText.Text = "";
            SaveButton.IsEnabled = true;
            StatusText.Text = $"Started new Item={nextId}.";
        }

        private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
            {
                StatusText.Text = "item.ini was not found.";
                return;
            }

            if (ItemListGrid.SelectedItem is not ItemRow selectedItem)
            {
                StatusText.Text = "Select an item before deleting.";
                return;
            }

            MessageBoxResult confirm = System.Windows.MessageBox.Show(
                $"Delete Item={selectedItem.Id} ({selectedItem.Name})?",
                "Delete item",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                _itemEditorService.DeleteItem(_itemIniPath, selectedItem.Id);
                ReloadWorkspaceAfterSave();
                ItemListGrid.ItemsSource = null;
                LoadItemRowsIfNeeded();
                StatusText.Text = $"Deleted Item={selectedItem.Id}.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Delete failed: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Delete item failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void NormalizeItemReferenceFieldsBeforeSave()
        {
            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
                return;

            List<ItemRow> items = _itemEditorService.LoadItemRows(_itemIniPath);

            foreach (ItemFieldRow row in _itemFieldRows.Where(row => IsItemItemReferenceField(row.Label)))
            {
                string raw = row.Value?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                if (int.TryParse(raw, out _))
                    continue;

                ItemRow? match = items.FirstOrDefault(item =>
                    item.Name.Equals(raw, StringComparison.OrdinalIgnoreCase));

                if (match is null)
                    continue;

                row.Value = match.Id.ToString();
                row.Enabled = true;
            }
        }
        private void SaveItem()
        {
            if (string.IsNullOrWhiteSpace(_itemIniPath) || !File.Exists(_itemIniPath))
            {
                StatusText.Text = "item.ini was not found.";
                return;
            }

            try
            {
                CommitItemGrids();
                NormalizeItemReferenceFieldsBeforeSave();
                NormalizeMonsterReferenceFieldsBeforeSave();
                _itemEditorService.SaveItemRows(_itemIniPath, _itemFieldRows.ToList());
                int savedId = int.Parse(_itemFieldRows.First(row => row.Label == "ID").Value);

                ReloadWorkspaceAfterSave();
                ItemListGrid.ItemsSource = null;
                LoadItemRowsIfNeeded();
                RefreshItemFilter();

                ItemRow? savedRow = _filteredItemRows.FirstOrDefault(row => row.Id == savedId);
                if (savedRow is not null)
                {
                    ItemListGrid.SelectedItem = savedRow;
                    LoadSelectedItemFields(savedId);
                }

                StatusText.Text = $"Saved Item={savedId}.";
                System.Windows.MessageBox.Show($"Saved Item={savedId}.", "Item saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Save item.ini failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void NormalizeMonsterReferenceFieldsBeforeSave()
        {
            if (string.IsNullOrWhiteSpace(_monsterIniPath) || !File.Exists(_monsterIniPath))
                return;

            List<MonsterRow> monsters = _monsterEditorService.LoadMonsterRows(_monsterIniPath);

            foreach (ItemFieldRow row in _itemFieldRows.Where(row => IsItemMonsterReferenceField(row.Label)))
            {
                string raw = row.Value?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                if (int.TryParse(raw, out _))
                    continue;

                MonsterRow? match = monsters.FirstOrDefault(monster =>
                    monster.Name.Equals(raw, StringComparison.OrdinalIgnoreCase));

                if (match is null)
                    continue;

                row.Value = match.Id.ToString();
                row.Enabled = true;
            }
        }
        private void CommitItemGrids()
        {
            DataGrid[] grids =
            {
                ItemBaseFieldGrid, ItemGeneralFieldGrid, ItemEquipmentFieldGrid, ItemAnimationsFieldGrid,
                ItemArmorFieldGrid, ItemWeaponFieldGrid, ItemGrowthFieldGrid, ItemFoodFieldGrid,
                ItemBonusFieldGrid, ItemSkillFieldGrid, ItemTraderFieldGrid, ItemMobsFieldGrid,
                ItemProximityFieldGrid, ItemMiscFieldGrid
            };

            foreach (DataGrid grid in grids)
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }

        private void ClearItemEditorOnly()
        {
            _selectedItemId = null;
            _itemFieldRows = new ObservableCollection<ItemFieldRow>();
            LoadItemFieldRowsIntoTabs(new List<ItemFieldRow>());
            SelectedItemText.Text = "No item selected.";
            ItemRawPreviewText.Text = "";
        }
        #endregion

        #region Multi-use
        private string? _multiUseIniPath;

        private ObservableCollection<MultiUseRow> _multiUseRows = new();
        private ObservableCollection<MultiUseRow> _filteredMultiUseRows = new();
        private ObservableCollection<MultiUseFieldRow> _multiUseFieldRows = new();
        private int? _selectedMultiUseId;


        private void LoadMultiUseRowsIfNeeded()
        {
            if (MultiUseListGrid.ItemsSource is not null)
                return;

            if (string.IsNullOrWhiteSpace(_multiUseIniPath) ||
                !File.Exists(_multiUseIniPath))
            {
                StatusText.Text = "multiuse.ini was not found.";
                return;
            }

            try
            {
                List<MultiUseRow> rows = _multiUseEditorService.LoadMultiUseRows(_multiUseIniPath);

                _multiUseRows = new ObservableCollection<MultiUseRow>(rows);
                _filteredMultiUseRows = new ObservableCollection<MultiUseRow>(rows);

                MultiUseListGrid.ItemsSource = _filteredMultiUseRows;
                MultiUseFieldGrid.ItemsSource = null;
                _selectedMultiUseId = null;
                SelectedMultiUseText.Text = "No multi-use recipe selected.";

                StatusText.Text = $"Loaded multiuse.ini editor with {_multiUseRows.Count} multi-use recipe(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load multiuse.ini: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load multiuse.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveMultiUse()
        {
            if (string.IsNullOrWhiteSpace(_multiUseIniPath) ||
                !File.Exists(_multiUseIniPath))
            {
                StatusText.Text = "multiuse.ini was not found.";
                return;
            }

            if (_selectedMultiUseId is null)
            {
                StatusText.Text = "Select a multi-use recipe before saving.";
                return;
            }

            try
            {
                MultiUseFieldGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                MultiUseFieldGrid.CommitEdit(DataGridEditingUnit.Row, true);

                SaveValidationResult result = _multiUseEditorService.SaveMultiUseRows(
                    _multiUseIniPath,
                    _selectedMultiUseId.Value,
                    _multiUseFieldRows.ToList());

                if (!result.Success)
                {
                    StatusText.Text = $"Save failed: {result.ErrorMessage}";

                    System.Windows.MessageBox.Show(
                        result.ErrorMessage ?? "Save failed.",
                        "Save failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                StatusText.Text =
                    $"{result.Message} Backup: {result.BackupPath}";

                System.Windows.MessageBox.Show(
                    $"{result.Message}\n\nBackup created:\n{result.BackupPath}",
                    "Multi-use recipe saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                int savedMultiUseId = _selectedMultiUseId.Value;

                ReloadWorkspaceAfterSave();

                MultiUseListGrid.ItemsSource = null;
                MultiUseFieldGrid.ItemsSource = null;

                LoadMultiUseRowsIfNeeded();

                MultiUseRow? savedRow = _filteredMultiUseRows.FirstOrDefault(row => row.Id == savedMultiUseId);

                if (savedRow is not null)
                {
                    MultiUseListGrid.SelectedItem = savedRow;
                    LoadSelectedMultiUseFields(savedMultiUseId);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Save multiuse.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void RefreshMultiUseFilter()
        {
            string needle = MultiUseSearchBox.Text.Trim();

            IEnumerable<MultiUseRow> filtered = _multiUseRows;

            if (!string.IsNullOrWhiteSpace(needle))
            {
                filtered = filtered.Where(recipe =>
                    recipe.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    recipe.SuccessItem.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    recipe.FocusItem.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    recipe.Skill.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            _filteredMultiUseRows = new ObservableCollection<MultiUseRow>(filtered);

            MultiUseListGrid.ItemsSource = _filteredMultiUseRows;
        }

        private void MultiUseSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MultiUseListGrid is null)
                return;

            RefreshMultiUseFilter();
        }

        private void MultiUseListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MultiUseListGrid.SelectedItem is not MultiUseRow selectedRecipe)
                return;

            LoadSelectedMultiUseFields(selectedRecipe.Id);
        }

        private void LoadSelectedMultiUseFields(int multiUseId)
        {
            if (string.IsNullOrWhiteSpace(_multiUseIniPath) ||
                !File.Exists(_multiUseIniPath))
            {
                StatusText.Text = "multiuse.ini was not found.";
                return;
            }

            try
            {
                List<MultiUseFieldRow> rows = _multiUseEditorService.LoadFieldRows(
                    _multiUseIniPath,
                    multiUseId);

                _selectedMultiUseId = multiUseId;
                _multiUseFieldRows = new ObservableCollection<MultiUseFieldRow>(rows);

                MultiUseFieldGrid.ItemsSource = _multiUseFieldRows;
                int expectedRecipeCount = _workspace?.MultiUses?.Recipes.Count ?? _multiUseRows.Count;

                MultiUseRawPreviewText.Text = _previewService.GetMultiUsePreview(
                    _multiUseIniPath,
                    multiUseId,
                    expectedRecipeCount);

                MultiUseRow? recipe = _multiUseRows.FirstOrDefault(row => row.Id == multiUseId);

                SelectedMultiUseText.Text = recipe is null
                    ? $"MultiUse {multiUseId}"
                    : $"MultiUse {recipe.Id}: {recipe.DisplayName}";

                SaveButton.IsEnabled = true;

                StatusText.Text = $"Loaded MultiUse={multiUseId} with {_multiUseFieldRows.Count} editable row(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load MultiUse={multiUseId}: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load multi-use recipe failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ShowMultiUseView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Collapsed;
            TreasureView.Visibility = Visibility.Collapsed;
            MagicView.Visibility = Visibility.Collapsed;
            MonsterView.Visibility = Visibility.Collapsed;
            ItemView.Visibility = Visibility.Collapsed;
            UsageView.Visibility = Visibility.Collapsed;
            MultiUseView.Visibility = Visibility.Visible;
            PlaceholderView.Visibility = Visibility.Collapsed;

            ExportCsvButton.IsEnabled = _multiUseIniPath is not null && File.Exists(_multiUseIniPath);

            LoadMultiUseRowsIfNeeded();

            SaveButton.IsEnabled = MultiUseFieldGrid.ItemsSource is not null;

            StatusText.Text = "Multi Use editor loaded.";
        }



        #endregion

        #region Usage
        private string? _usageIniPath;

        private ObservableCollection<UsageRow> _usageRows = new();
        private ObservableCollection<UsageRow> _filteredUsageRows = new();
        private ObservableCollection<UsageFieldRow> _usageFieldRows = new();
        private int? _selectedUsageId;

        private void SaveUsage()
        {
            if (string.IsNullOrWhiteSpace(_usageIniPath) ||
                !File.Exists(_usageIniPath))
            {
                StatusText.Text = "itemuse.ini was not found.";
                return;
            }

            if (_selectedUsageId is null)
            {
                StatusText.Text = "Select an item use entry before saving.";
                return;
            }

            try
            {
                UsageFieldGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                UsageFieldGrid.CommitEdit(DataGridEditingUnit.Row, true);

                SaveValidationResult result = _usageEditorService.SaveUsageRows(
                    _usageIniPath,
                    _selectedUsageId.Value,
                    _usageFieldRows.ToList());

                if (!result.Success)
                {
                    StatusText.Text = $"Save failed: {result.ErrorMessage}";

                    System.Windows.MessageBox.Show(
                        result.ErrorMessage ?? "Save failed.",
                        "Save failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                StatusText.Text =
                    $"{result.Message} Backup: {result.BackupPath}";

                System.Windows.MessageBox.Show(
                    $"{result.Message}\n\nBackup created:\n{result.BackupPath}",
                    "Item use saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                int savedUsageId = _selectedUsageId.Value;

                ReloadWorkspaceAfterSave();

                UsageListGrid.ItemsSource = null;
                UsageFieldGrid.ItemsSource = null;

                LoadUsageRowsIfNeeded();

                UsageRow? savedRow = _filteredUsageRows.FirstOrDefault(row => row.Id == savedUsageId);

                if (savedRow is not null)
                {
                    UsageListGrid.SelectedItem = savedRow;
                    LoadSelectedUsageFields(savedUsageId);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Save itemuse.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void ShowUsageView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Collapsed;
            TreasureView.Visibility = Visibility.Collapsed;
            MagicView.Visibility = Visibility.Collapsed;
            MonsterView.Visibility = Visibility.Collapsed;
            ItemView.Visibility = Visibility.Collapsed;
            UsageView.Visibility = Visibility.Visible;
            MultiUseView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;

            ExportCsvButton.IsEnabled = _usageIniPath is not null && File.Exists(_usageIniPath);

            LoadUsageRowsIfNeeded();

            SaveButton.IsEnabled = UsageFieldGrid.ItemsSource is not null;

            StatusText.Text = "Item Use editor loaded.";
        }


        private void LoadUsageRowsIfNeeded()
        {
            if (UsageListGrid.ItemsSource is not null)
                return;

            if (string.IsNullOrWhiteSpace(_usageIniPath) ||
                !File.Exists(_usageIniPath))
            {
                StatusText.Text = "itemuse.ini was not found.";
                return;
            }

            try
            {
                List<UsageRow> rows = _usageEditorService.LoadUsageRows(_usageIniPath);

                _usageRows = new ObservableCollection<UsageRow>(rows);
                _filteredUsageRows = new ObservableCollection<UsageRow>(rows);

                UsageListGrid.ItemsSource = _filteredUsageRows;
                UsageFieldGrid.ItemsSource = null;
                _selectedUsageId = null;
                SelectedUsageText.Text = "No item use selected.";

                StatusText.Text = $"Loaded itemuse.ini editor with {_usageRows.Count} item use entrie(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load itemuse.ini: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load itemuse.ini failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RefreshUsageFilter()
        {
            string needle = UsageSearchBox.Text.Trim();

            IEnumerable<UsageRow> filtered = _usageRows;

            if (!string.IsNullOrWhiteSpace(needle))
            {
                filtered = filtered.Where(usage =>
                    usage.Id.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    usage.ItemTool.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    usage.ItemFocus.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    usage.Skill.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            _filteredUsageRows = new ObservableCollection<UsageRow>(filtered);

            UsageListGrid.ItemsSource = _filteredUsageRows;
        }

        private void UsageSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UsageListGrid is null)
                return;

            RefreshUsageFilter();
        }

        private void UsageListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsageListGrid.SelectedItem is not UsageRow selectedUsage)
                return;

            LoadSelectedUsageFields(selectedUsage.Id);
        }

        private void LoadSelectedUsageFields(int usageId)
        {
            if (string.IsNullOrWhiteSpace(_usageIniPath) ||
                !File.Exists(_usageIniPath))
            {
                StatusText.Text = "itemuse.ini was not found.";
                return;
            }

            try
            {
                List<UsageFieldRow> rows = _usageEditorService.LoadFieldRows(
                    _usageIniPath,
                    usageId);

                _selectedUsageId = usageId;
                _usageFieldRows = new ObservableCollection<UsageFieldRow>(rows);

                UsageFieldGrid.ItemsSource = _usageFieldRows;
                UsageRawPreviewText.Text = _previewService.GetUsagePreview(
                                                                _usageIniPath,
                                                                usageId);

                UsageRow? usage = _usageRows.FirstOrDefault(row => row.Id == usageId);

                SelectedUsageText.Text = usage is null
                    ? $"ItemUse {usageId}"
                    : $"ItemUse {usage.Id}: {usage.DisplayName}";

                SaveButton.IsEnabled = true;

                StatusText.Text = $"Loaded ItemUse={usageId} with {_usageFieldRows.Count} editable row(s).";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load ItemUse={usageId}: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Load item use failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        #endregion


        private static IEnumerable<DependencyObject> GetVisualChildren(DependencyObject parent)
        {
            if (parent == null)
                yield break;

            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                yield return child;

                foreach (DependencyObject descendant in GetVisualChildren(child))
                    yield return descendant;
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "Select your RPGWO server folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            WinForms.DialogResult result = dialog.ShowDialog();

            if (result != WinForms.DialogResult.OK ||
                string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                return;
            }

            LoadServerFolder(dialog.SelectedPath);
        }

        private void LoadServerFolder(string folderPath)
        {
            try
            {
                _serverFolderPath = folderPath;
                _worldIniPath = Path.Combine(folderPath, "world.ini");
                _skillIniPath = Path.Combine(folderPath, "skill.ini");
                _animationIniPath = Path.Combine(folderPath, "animation.ini");
                _treasureIniPath = Path.Combine(folderPath, "treasure.ini");
                _magicIniPath = Path.Combine(folderPath, "magic.ini");
                _monsterIniPath = Path.Combine(folderPath, "monster.ini");
                _itemIniPath = Path.Combine(folderPath, "item.ini");
                _usageIniPath = Path.Combine(folderPath, "itemuse.ini");
                _multiUseIniPath = Path.Combine(folderPath, "multiuse.ini");

                ServerFolderText.Text = folderPath;

                _workspace = _workspaceService.Load(folderPath);

                StatusGrid.ItemsSource = _workspace.FileStatuses;

                int ready = _workspace.FileStatuses.Count(status => status.StatusText == "Ready");
                int missing = _workspace.FileStatuses.Count(status => !status.Exists);
                int issues = _workspace.FileStatuses.Count(status => status.IssueCount > 0);
                int unknowns = _workspace.FileStatuses.Count(status => status.UnknownCount > 0);

                SaveButton.IsEnabled = false;
                ExportCsvButton.IsEnabled = false;

                CommitWorldGrids();
                SkillListGrid.ItemsSource = null;
                ClearSkillEditorOnly();
                AnimationListGrid.ItemsSource = null;
                AnimationFieldGrid.ItemsSource = null;
                _selectedAnimationId = null;
                SelectedAnimationText.Text = "No animation selected.";
                TreasureListGrid.ItemsSource = null;
                TreasureEntryGrid.ItemsSource = null;
                TreasureEntryFieldGrid.ItemsSource = null;

                _treasureEntryRows.Clear();
                _selectedTreasureEntryFieldRows.Clear();

                _selectedTreasureId = null;
                _selectedTreasureEntryIndex = null;

                SelectedTreasureTableText.Text = "No treasure table selected.";
                SelectedTreasureEntryText.Text = "No treasure entry selected.";
                MagicListGrid.ItemsSource = null;
                ClearMagicEditorOnly();
                _selectedMagicId = null;
                SelectedMagicText.Text = "No spell selected.";
                MonsterListGrid.ItemsSource = null;
                ClearMonsterEditorOnly();
                _selectedMonsterId = null;
                SelectedMonsterText.Text = "No monster selected.";
                ItemListGrid.ItemsSource = null;
                ClearItemEditorOnly();
                _selectedItemId = null;
                SelectedItemText.Text = "No item selected.";
                UsageListGrid.ItemsSource = null;
                UsageFieldGrid.ItemsSource = null;
                _selectedUsageId = null;
                SelectedUsageText.Text = "No item use selected.";
                MultiUseListGrid.ItemsSource = null;
                MultiUseFieldGrid.ItemsSource = null;
                _selectedMultiUseId = null;
                SelectedMultiUseText.Text = "No multi-use recipe selected.";

                WorldRawPreviewText.Text = "";
                SkillRawPreviewText.Text = "";
                AnimationRawPreviewText.Text = "";
                MagicRawPreviewText.Text = "";
                MonsterRawPreviewText.Text = "";
                ItemRawPreviewText.Text = "";
                UsageRawPreviewText.Text = "";
                MultiUseRawPreviewText.Text = "";

                EnableNavigationFromWorkspace();

                StatusText.Text =
                    $"Scan complete. Ready: {ready}, Missing: {missing}, Unknowns: {unknowns}, Issues: {issues}.";

                ShowDashboardView();
                SelectNavigation("Dashboard");
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Scan failed: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Scan failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EnableNavigationFromWorkspace()
        {
            if (_workspace is null)
                return;

            SetNavigationEnabled("World", _workspace.GetStatus(SupportedServerFile.World)?.Exists == true);
            SetNavigationEnabled("Skills", _workspace.GetStatus(SupportedServerFile.Skills)?.Exists == true);
            SetNavigationEnabled("Animations", _workspace.GetStatus(SupportedServerFile.Animations)?.Exists == true);
            SetNavigationEnabled("Treasures", _workspace.GetStatus(SupportedServerFile.Treasures)?.Exists == true);
            SetNavigationEnabled("Magic", _workspace.GetStatus(SupportedServerFile.Magic)?.Exists == true);
            SetNavigationEnabled("Monsters", _workspace.GetStatus(SupportedServerFile.Monsters)?.Exists == true);
            SetNavigationEnabled("Items", _workspace.GetStatus(SupportedServerFile.Items)?.Exists == true);
            SetNavigationEnabled("Usages", _workspace.GetStatus(SupportedServerFile.Usages)?.Exists == true);
            SetNavigationEnabled("MultiUses", _workspace.GetStatus(SupportedServerFile.MultiUses)?.Exists == true);
        }

        private void SetNavigationEnabled(string tag, bool enabled)
        {
            foreach (object item in NavigationList.Items)
            {
                if (item is not ListBoxItem listBoxItem)
                    continue;

                if ((listBoxItem.Tag?.ToString() ?? "").Equals(tag, StringComparison.OrdinalIgnoreCase))
                {
                    listBoxItem.IsEnabled = enabled;
                    return;
                }
            }
        }

        private void SelectNavigation(string tag)
        {
            foreach (object item in NavigationList.Items)
            {
                if (item is not ListBoxItem listBoxItem)
                    continue;

                if ((listBoxItem.Tag?.ToString() ?? "").Equals(tag, StringComparison.OrdinalIgnoreCase))
                {
                    NavigationList.SelectedItem = listBoxItem;
                    return;
                }
            }
        }

        private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
                return;

            if (NavigationList.SelectedItem is not ListBoxItem item)
                return;

            string selected = item.Tag?.ToString() ?? "";

            switch (selected)
            {
                case "Dashboard":
                    ShowDashboardView();
                    break;

                case "World":
                    ShowWorldView();
                    break;
                case "Skills":
                    ShowSkillView();
                    break;
                case "Animations":
                    ShowAnimationView();
                    break;
                case "Treasures":
                    ShowTreasureView();
                    break;
                case "Magic":
                    ShowMagicView();
                    break;
                case "Monsters":
                    ShowMonsterView();
                    break;
                case "Items":
                    ShowItemView();
                    break;
                case "Usages":
                    ShowUsageView();
                    break;
                case "MultiUses":
                    ShowMultiUseView();
                    break;

                default:
                    ShowPlaceholderView(item.Content?.ToString() ?? selected);
                    break;
            }
        }

        private void StatusGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusGrid.SelectedItem is not ServerFileStatus status)
                return;

            ExportCsvButton.IsEnabled = status.Exists && status.Loaded;

            StatusText.Text = $"{status.DisplayName}: {status.Summary}";
        }

        private void ShowDashboardView()
        {
            DashboardView.Visibility = Visibility.Visible;
            WorldView.Visibility = Visibility.Collapsed;
            SkillView.Visibility = Visibility.Collapsed;
            AnimationView.Visibility = Visibility.Collapsed;
            TreasureView.Visibility = Visibility.Collapsed;
            MagicView.Visibility = Visibility.Collapsed;
            MonsterView.Visibility = Visibility.Collapsed;
            ItemView.Visibility = Visibility.Collapsed;
            UsageView.Visibility = Visibility.Collapsed;
            MultiUseView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;

            SaveButton.IsEnabled = false;

            if (StatusGrid.SelectedItem is ServerFileStatus status)
                ExportCsvButton.IsEnabled = status.Exists && status.Loaded;
            else
                ExportCsvButton.IsEnabled = false;
        }

        private void OpenSpriteSheetEditor_Click(object sender, RoutedEventArgs e)
        {
            var window = new SpriteSheetEditorWindow
            {
                Owner = this
            };

            window.Show();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (WorldView.Visibility == Visibility.Visible)
            {
                SaveWorld();
                return;
            }

            if (SkillView.Visibility == Visibility.Visible)
            {
                SaveSkill();
                return;
            }

            if (AnimationView.Visibility == Visibility.Visible)
            {
                SaveAnimation();
                return;
            }

            if (TreasureView.Visibility == Visibility.Visible)
            {
                SaveTreasure();
                return;
            }

            if (MagicView.Visibility == Visibility.Visible)
            {
                SaveMagic();
                return;
            }

            if (MonsterView.Visibility == Visibility.Visible)
            {
                SaveMonster();
                return;
            }
            if (ItemView.Visibility == Visibility.Visible)
            {
                SaveItem();
                return;
            }

            if (UsageView.Visibility == Visibility.Visible)
            {
                SaveUsage();
                return;
            }

            if (MultiUseView.Visibility == Visibility.Visible)
            {
                SaveMultiUse();
                return;
            }


            StatusText.Text = "There is nothing to save for the current view.";
        }

        private void ReloadWorkspaceAfterSave()
        {
            if (string.IsNullOrWhiteSpace(_serverFolderPath))
                return;

            _workspace = _workspaceService.Load(_serverFolderPath);
            StatusGrid.ItemsSource = _workspace.FileStatuses;
            EnableNavigationFromWorkspace();
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            ServerFileStatus? status = GetExportTarget();

            if (status is null)
            {
                StatusText.Text = "Select a file first.";
                return;
            }

            if (!status.Exists || !status.Loaded)
            {
                StatusText.Text = "Selected file is not available for export.";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = $"Export {status.DisplayName} to CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = Path.GetFileNameWithoutExtension(status.FileName) + ".csv",
                AddExtension = true,
                DefaultExt = ".csv"
            };

            bool? result = dialog.ShowDialog();

            if (result != true ||
                string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            try
            {
                _csvExportService.Export(
                    status.FileType,
                    status.FilePath,
                    dialog.FileName);

                StatusText.Text = $"Exported {status.DisplayName} CSV to: {dialog.FileName}";

                System.Windows.MessageBox.Show(
                    $"CSV export complete:\n{dialog.FileName}",
                    "Export complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Export failed: {ex.Message}";

                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Export failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private ServerFileStatus? GetExportTarget()
        {
            if (DashboardView.Visibility == Visibility.Visible &&
                StatusGrid.SelectedItem is ServerFileStatus selectedStatus)
            {
                return selectedStatus;
            }

            if (WorldView.Visibility == Visibility.Visible &&
                _workspace is not null)
            {
                return _workspace.GetStatus(SupportedServerFile.World);
            }

            if (SkillView.Visibility == Visibility.Visible &&
                _workspace is not null)
            {
                return _workspace.GetStatus(SupportedServerFile.Skills);
            }
            if (AnimationView.Visibility == Visibility.Visible &&
                _workspace is not null)
            {
                return _workspace.GetStatus(SupportedServerFile.Animations);
            }

            if (TreasureView.Visibility == Visibility.Visible &&
                _workspace is not null)
            {
                return _workspace.GetStatus(SupportedServerFile.Treasures);
            }

            if (MagicView.Visibility == Visibility.Visible &&
                _workspace is not null)
            {
                return _workspace.GetStatus(SupportedServerFile.Magic);
            }

            if (MonsterView.Visibility == Visibility.Visible &&
                _workspace is not null)
            {
                return _workspace.GetStatus(SupportedServerFile.Monsters);
            }

            if (ItemView.Visibility == Visibility.Visible &&
                _workspace is not null)
            {
                return _workspace.GetStatus(SupportedServerFile.Items);
            }

            if (UsageView.Visibility == Visibility.Visible &&
                _workspace is not null)
            {
                return _workspace.GetStatus(SupportedServerFile.Usages);
            }

            if (MultiUseView.Visibility == Visibility.Visible &&
                _workspace is not null)
            {
                return _workspace.GetStatus(SupportedServerFile.MultiUses);
            }

            return null;
        }
    }
}