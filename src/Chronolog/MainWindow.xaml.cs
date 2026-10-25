using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Chronolog.Views;
using Chronolog.Models;
using Chronolog.Services;
using Chronolog.Themes;

namespace Chronolog;

public partial class MainWindow : Window
{
    private readonly AppSettingsStore _settingsStore = new();
    private AppSettings _settings = new();
    private JournalStore _store;
    private ChronologData _data = new();
    private Journal? _currentJournal;
    private readonly ObservableCollection<Journal> _journals = [];
    private readonly ObservableCollection<JournalEntry> _visibleEntries = [];
    private bool _suppressSelectionEvents;

    public MainWindow()
    {
        _settings = _settingsStore.Load();
        _store = new JournalStore(_settings.DataDirectory ?? _settingsStore.DefaultDataDirectory);

        InitializeComponent();
        Title = AppEdition.ProductName;
        JournalTabs.ItemsSource = _journals;
        EntryList.ItemsSource = _visibleEntries;
        BuildThemeMenu();

        CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (_, _) => NewEntry_Click(this, new RoutedEventArgs())));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (_, _) => Save_Click(this, new RoutedEventArgs())));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (_, _) => FocusSearch_Click(this, new RoutedEventArgs())));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, (_, _) => DeleteEntry_Click(this, new RoutedEventArgs())));

        InputBindings.Add(new KeyBinding(ApplicationCommands.New, Key.N, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Save, Key.S, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Find, Key.F, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Delete, Key.Delete, ModifierKeys.None));
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadLibrary();
        SetStatus("Ready");
    }

    private void LoadLibrary()
    {
        _data = _store.Load();

        ApplyTheme(_data.ThemeId, persist: false);
        RebuildJournalTabs();

        var selectId = _data.LastSelectedJournalId
                       ?? _data.Journals.FirstOrDefault()?.Id;

        if (selectId is Guid id)
            SelectJournalTab(id);
        else
        {
            _currentJournal = null;
            RefreshEntryList();
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        Persist();
    }

    private void Persist()
    {
        if (_currentJournal is not null)
            _data.LastSelectedJournalId = _currentJournal.Id;

        _data.ThemeId = ThemeService.Current.Id;
        _store.Save(_data);
    }

    private void BuildThemeMenu()
    {
        ThemeMenu.Items.Clear();

        string? lastEra = null;
        foreach (var theme in ThemeCatalog.All)
        {
            if (lastEra is not null && lastEra != theme.Era)
                ThemeMenu.Items.Add(new Separator());

            lastEra = theme.Era;

            var item = new MenuItem
            {
                Header = $"{theme.DisplayName}  ({theme.Era})",
                Tag = theme.Id,
                IsCheckable = true,
                Style = (Style)FindResource("SubMenuItem")
            };
            item.Click += ThemeMenuItem_Click;
            ThemeMenu.Items.Add(item);
        }
    }

    private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string themeId })
            return;

        ApplyTheme(themeId, persist: true);
        SetStatus("Ready");
    }

    private void ApplyTheme(string? themeId, bool persist)
    {
        ThemeService.Apply(themeId);

        foreach (var obj in ThemeMenu.Items)
        {
            if (obj is MenuItem mi && mi.Tag is string id)
                mi.IsChecked = string.Equals(id, ThemeService.Current.Id, StringComparison.OrdinalIgnoreCase);
        }

        if (persist)
        {
            _data.ThemeId = ThemeService.Current.Id;
            _store.Save(_data);
        }
    }

    private void RebuildJournalTabs()
    {
        _suppressSelectionEvents = true;
        try
        {
            _journals.Clear();
            foreach (var journal in _data.Journals)
                _journals.Add(journal);
        }
        finally
        {
            _suppressSelectionEvents = false;
        }
    }

    private void SelectJournalTab(Guid journalId)
    {
        var match = _journals.FirstOrDefault(j => j.Id == journalId);
        if (match is not null)
        {
            JournalTabs.SelectedItem = match;
            return;
        }

        if (_journals.Count > 0)
            JournalTabs.SelectedIndex = 0;
    }

    private void JournalTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionEvents)
            return;

        if (JournalTabs.SelectedItem is not Journal journal)
            return;

        SwitchToJournal(journal);
    }

    private void SwitchToJournal(Journal journal)
    {
        _currentJournal = journal;
        Title = $"{AppEdition.ProductName} — {journal.TabTitle}";
        SearchBox.Text = string.Empty;
        RefreshEntryList();
        SetStatus("Ready");
    }

    private void RefreshEntryList(Guid? selectEntryId = null)
    {
        if (_currentJournal is null)
        {
            _visibleEntries.Clear();
            UpdateReadingPane(null);
            return;
        }

        var query = SearchBox.Text?.Trim() ?? string.Empty;
        IEnumerable<JournalEntry> entries = _currentJournal.Entries
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt);

        if (!string.IsNullOrWhiteSpace(query))
        {
            entries = entries.Where(e =>
                e.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Body.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.DateLabel.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Attachments.Any(a =>
                    a.FileName.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        var list = entries.ToList();
        var previouslySelected = selectEntryId
            ?? (EntryList.SelectedItem as JournalEntry)?.Id;

        _suppressSelectionEvents = true;
        try
        {
            _visibleEntries.Clear();
            foreach (var entry in list)
                _visibleEntries.Add(entry);

            JournalEntry? toSelect = null;
            if (previouslySelected is Guid pid)
                toSelect = _visibleEntries.FirstOrDefault(e => e.Id == pid);

            toSelect ??= _visibleEntries.FirstOrDefault();
            EntryList.SelectedItem = toSelect;
        }
        finally
        {
            _suppressSelectionEvents = false;
        }

        UpdateReadingPane(EntryList.SelectedItem as JournalEntry);
    }

    private void UpdateReadingPane(JournalEntry? entry)
    {
        ImagePreviewPanel.Children.Clear();
        EntryAttachmentList.ItemsSource = null;

        if (entry is null)
        {
            EntryTitleText.Text = "No entry selected";
            EntryMetaText.Text = string.Empty;
            EntryBodyBox.Text = string.Empty;
            AttachmentStrip.Visibility = Visibility.Collapsed;
            EmptyHint.Visibility = _visibleEntries.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            EntryBodyBox.Visibility = EmptyHint.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
            return;
        }

        EmptyHint.Visibility = Visibility.Collapsed;
        EntryBodyBox.Visibility = Visibility.Visible;

        EntryTitleText.Text = string.IsNullOrWhiteSpace(entry.Title)
            ? entry.DateLabel
            : entry.Title;

        var meta = entry.EntryDate.ToString("dddd, MMMM d, yyyy");
        if (entry.UpdatedAt.Date != entry.CreatedAt.Date ||
            Math.Abs((entry.UpdatedAt - entry.CreatedAt).TotalMinutes) > 1)
        {
            meta += $"  ·  last edited {entry.UpdatedAt:g}";
        }

        if (entry.Attachments.Count > 0)
            meta += $"  ·  {entry.Attachments.Count} attachment{(entry.Attachments.Count == 1 ? "" : "s")}";

        EntryMetaText.Text = meta;
        EntryBodyBox.Text = entry.Body;
        LoadAttachments(entry);
        EntryScroll.ScrollToHome();
    }

    private void LoadAttachments(JournalEntry entry)
    {
        entry.Attachments ??= [];

        if (entry.Attachments.Count == 0)
        {
            AttachmentStrip.Visibility = Visibility.Collapsed;
            return;
        }

        AttachmentStrip.Visibility = Visibility.Visible;
        EntryAttachmentList.ItemsSource = entry.Attachments;

        foreach (var attachment in entry.Attachments.Where(a => a.IsImage))
        {
            try
            {
                var path = _store.Attachments.GetFullPath(entry.Id, attachment);
                if (!File.Exists(path))
                    continue;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.DecodePixelWidth = 240;
                bitmap.EndInit();
                bitmap.Freeze();

                var border = new Border
                {
                    BorderBrush = (Brush)FindResource("BorderBrushKey"),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 8, 8, 0),
                    CornerRadius = (CornerRadius)FindResource("ButtonCornerRadius"),
                    Cursor = Cursors.Hand,
                    ToolTip = attachment.FileName,
                    Tag = attachment
                };

                var image = new Image
                {
                    Source = bitmap,
                    MaxHeight = 160,
                    MaxWidth = 240,
                    Stretch = Stretch.Uniform
                };

                border.Child = image;
                border.MouseLeftButtonUp += (_, _) =>
                {
                    try { _store.Attachments.Open(entry.Id, attachment); }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, "Chronolog",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };

                ImagePreviewPanel.Children.Add(border);
            }
            catch
            {
                // Skip unreadable images
            }
        }
    }

    private JournalEntry? CurrentReadingEntry =>
        EntryList.SelectedItem as JournalEntry;

    private void EntryAttachmentList_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        OpenSelectedAttachment_Click(sender, e);

    private void OpenSelectedAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentReadingEntry is null ||
            EntryAttachmentList.SelectedItem is not Attachment attachment)
            return;

        try
        {
            _store.Attachments.Open(CurrentReadingEntry.Id, attachment);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Chronolog",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RevealSelectedAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentReadingEntry is null ||
            EntryAttachmentList.SelectedItem is not Attachment attachment)
            return;

        try
        {
            _store.Attachments.RevealInExplorer(CurrentReadingEntry.Id, attachment);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Chronolog",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SetStatus(string? _ = null)
    {
        StatusText.Text = "Ready";
    }

    private void EntryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionEvents)
            return;

        UpdateReadingPane(EntryList.SelectedItem as JournalEntry);
    }

    private void EntryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EntryList.SelectedItem is JournalEntry)
            EditEntry_Click(sender, e);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
        RefreshEntryList();
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        SearchBox.Focus();
    }

    private void FocusSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void NewEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_currentJournal is null)
        {
            MessageBox.Show(this, "Create or select a journal first.", "Chronolog",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new WriteEntryWindow(_store.Attachments) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.ResultEntry is JournalEntry entry)
        {
            _currentJournal.Entries.Insert(0, entry);
            Persist();
            RefreshEntryList(entry.Id);
            var attachNote = entry.Attachments.Count > 0
                ? $" (+{entry.Attachments.Count} file{(entry.Attachments.Count == 1 ? "" : "s")})"
                : "";
            SetStatus($"Saved entry for {entry.DateLabel}{attachNote}");
        }
    }

    private void EditEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryList.SelectedItem is not JournalEntry entry)
        {
            MessageBox.Show(this, "Select an entry to edit.", "Chronolog",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new WriteEntryWindow(_store.Attachments, entry) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            Persist();
            RefreshEntryList(entry.Id);
            SetStatus($"Updated entry for {entry.DateLabel}");
        }
    }

    private void DeleteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_currentJournal is null || EntryList.SelectedItem is not JournalEntry entry)
            return;

        if (Keyboard.FocusedElement is TextBox { IsReadOnly: false })
            return;

        var label = string.IsNullOrWhiteSpace(entry.Title)
            ? entry.DateLabel
            : $"{entry.DateLabel} — {entry.Title}";

        var attachWarn = entry.Attachments.Count > 0
            ? $"\n\nThis will also delete {entry.Attachments.Count} attachment(s)."
            : "";

        var result = MessageBox.Show(
            this,
            $"Delete this journal entry?\n\n{label}{attachWarn}\n\nThis cannot be undone.",
            "Delete Entry",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
            return;

        _store.DeleteEntryAttachments(entry);
        _currentJournal.Entries.RemoveAll(x => x.Id == entry.Id);
        Persist();
        RefreshEntryList();
        SetStatus("Entry deleted.");
    }

    private void PreviousEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryList.SelectedIndex > 0)
            EntryList.SelectedIndex--;
    }

    private void NextEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryList.SelectedIndex >= 0 &&
            EntryList.SelectedIndex < EntryList.Items.Count - 1)
        {
            EntryList.SelectedIndex++;
        }
    }

    private void NewJournal_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewJournalWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.ResultJournal is not Journal journal)
            return;

        _data.Journals.Add(journal);
        Persist();
        RebuildJournalTabs();
        SelectJournalTab(journal.Id);
        SetStatus($"Created journal “{journal.TabTitle}”");
    }

    private void RenameJournal_Click(object sender, RoutedEventArgs e)
    {
        if (_currentJournal is null)
            return;

        var name = Prompt(
            "Rename Journal",
            "Journal name:",
            _currentJournal.Name);
        if (name is null)
            return;

        var owner = Prompt(
            "Rename Journal",
            "Owner name (optional):",
            _currentJournal.Owner);
        if (owner is null)
            return;

        _currentJournal.Name = string.IsNullOrWhiteSpace(name) ? _currentJournal.Name : name.Trim();
        _currentJournal.Owner = owner.Trim();
        Persist();

        var id = _currentJournal.Id;
        RebuildJournalTabs();
        SelectJournalTab(id);
        Title = $"{AppEdition.ProductName} — {_currentJournal.TabTitle}";
        SetStatus("Journal renamed.");
    }

    private void DeleteJournal_Click(object sender, RoutedEventArgs e)
    {
        if (_currentJournal is null)
            return;

        if (_data.Journals.Count <= 1)
        {
            MessageBox.Show(
                this,
                "You must keep at least one journal.",
                "Chronolog",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Delete journal “{_currentJournal.TabTitle}” and all of its {_currentJournal.Entries.Count} entries?\n\nThis cannot be undone.",
            "Delete Journal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
            return;

        var id = _currentJournal.Id;
        _data.Journals.RemoveAll(j => j.Id == id);
        _currentJournal = null;
        Persist();
        RebuildJournalTabs();
        if (_data.Journals.Count > 0)
            SelectJournalTab(_data.Journals[0].Id);
        SetStatus("Journal deleted.");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Persist();
        SetStatus($"Saved — {_store.DataPath}");
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = _store.DataDirectory;
        if (string.IsNullOrEmpty(folder))
            return;

        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private void StorageLocation_Click(object sender, RoutedEventArgs e)
    {
        // Save current library first so nothing is lost
        Persist();

        var dialog = new StorageLocationWindow(
            _store.DataDirectory,
            _settingsStore.DefaultDataDirectory)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedDirectory))
            return;

        var previous = _store.DataDirectory;
        var next = Path.GetFullPath(dialog.SelectedDirectory);

        try
        {
            if (dialog.ChangeMode is StorageChangeMode.Copy or StorageChangeMode.Move)
            {
                JournalStore.CopyLibrary(previous, next);
            }

            if (dialog.ChangeMode == StorageChangeMode.Move)
            {
                // Leave a pointer note in the old folder (do not force-delete user data)
                try
                {
                    var note = Path.Combine(previous, "MOVED.txt");
                    File.WriteAllText(note,
                        "This Chronolog library was moved.\n\n" +
                        $"New location:\n{next}\n\n" +
                        $"Moved at: {DateTime.Now:u}\n" +
                        "You may delete this folder after confirming the new library works.\n");
                }
                catch
                {
                    // non-fatal
                }
            }

            _settings.DataDirectory = next;
            _settingsStore.Save(_settings);
            _store.SetDataDirectory(next);
            LoadLibrary();

            var modeLabel = dialog.ChangeMode switch
            {
                StorageChangeMode.Copy => "copied and switched",
                StorageChangeMode.Move => "moved and switched",
                _ => "switched"
            };

            SetStatus($"Library {modeLabel}: {_store.DataDirectory}");
            MessageBox.Show(this,
                $"Library location updated.\n\n{_store.DataDirectory}\n\n" +
                "On other computers: install Chronolog → File → Storage Location → browse to this same folder → Switch only.",
                "Storage Location",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Could not change storage location:\n{ex.Message}",
                "Chronolog",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow(_store.DataDirectory) { Owner = this };
        about.ShowDialog();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private string? Prompt(string title, string label, string initial)
    {
        var window = new Window
        {
            Title = title,
            Width = 400,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = (Brush)FindResource("FaceBrush"),
            Foreground = (Brush)FindResource("InkBrush"),
            ShowInTaskbar = false
        };

        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var labelBlock = new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 6),
            FontFamily = (FontFamily)FindResource("UiFont"),
            Foreground = (Brush)FindResource("InkBrush"),
            FontSize = 12
        };
        Grid.SetRow(labelBlock, 0);

        var boxBorder = new Border
        {
            Style = (Style)FindResource("SearchBoxBorder")
        };
        var box = new TextBox
        {
            Text = initial,
            Style = (Style)FindResource("RetroTextBox")
        };
        boxBorder.Child = box;
        Grid.SetRow(boxBorder, 1);

        string? result = null;
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        Grid.SetRow(buttons, 3);

        var ok = new Button
        {
            Content = "OK",
            Width = 80,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true,
            Style = (Style)FindResource("PrimaryRetroButton")
        };
        ok.Click += (_, _) =>
        {
            result = box.Text;
            window.DialogResult = true;
            window.Close();
        };

        var cancel = new Button
        {
            Content = "Cancel",
            Width = 80,
            IsCancel = true,
            Style = (Style)FindResource("RetroButton")
        };
        cancel.Click += (_, _) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        root.Children.Add(labelBlock);
        root.Children.Add(boxBorder);
        root.Children.Add(buttons);
        window.Content = root;
        window.Loaded += (_, _) =>
        {
            box.Focus();
            box.SelectAll();
        };

        return window.ShowDialog() == true ? result : null;
    }
}
