using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Chronolog.Models;
using Chronolog.Services;
using Microsoft.Win32;

namespace Chronolog.Views;

public partial class WriteEntryWindow : Window
{
    public JournalEntry? ResultEntry { get; private set; }

    private readonly JournalEntry _entry;
    private readonly bool _isNew;
    private readonly AttachmentStore _attachments;
    private readonly ObservableCollection<Attachment> _list = [];
    private readonly List<Attachment> _addedThisSession = [];
    private readonly List<Attachment> _removedThisSession = [];
    private bool _committed;

    public WriteEntryWindow(AttachmentStore attachments, JournalEntry? existing = null)
    {
        InitializeComponent();
        _attachments = attachments;
        _isNew = existing is null;
        _entry = existing ?? new JournalEntry();

        AttachmentList.ItemsSource = _list;
        foreach (var a in _entry.Attachments)
            _list.Add(a);
        RefreshAttachmentHint();

        if (!_isNew)
        {
            Title = "Edit Journal Entry";
            TitleBox.Text = _entry.Title;
            BodyBox.Text = _entry.Body;
            DatePicker.SelectedDate = _entry.EntryDate;
            SaveButton.Content = "Save Changes";
        }
        else
        {
            DatePicker.SelectedDate = DateTime.Today;
            TitleBox.Focus();
        }
    }

    private void RefreshAttachmentHint()
    {
        NoAttachmentsHint.Visibility = _list.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void AttachFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Attach files to journal entry",
            Multiselect = true,
            Filter =
                "All supported|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.pdf;*.txt;*.md;*.doc;*.docx;*.xls;*.xlsx;*.zip;*.mp3;*.mp4|" +
                "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|" +
                "Documents|*.pdf;*.txt;*.md;*.doc;*.docx;*.xls;*.xlsx|" +
                "All files|*.*"
        };

        if (dialog.ShowDialog(this) == true)
            ImportFiles(dialog.FileNames);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return;

        ImportFiles(files);
    }

    private void ImportFiles(IEnumerable<string> paths)
    {
        var imported = 0;
        foreach (var path in paths)
        {
            try
            {
                if (Directory.Exists(path))
                    continue;

                var attachment = _attachments.Import(_entry.Id, path);
                _list.Add(attachment);
                _addedThisSession.Add(attachment);
                imported++;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Could not attach “{Path.GetFileName(path)}”:\n{ex.Message}",
                    "Chronolog",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        RefreshAttachmentHint();
        if (imported > 0)
            AttachmentList.SelectedItem = _list[_list.Count - 1];
    }

    private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (AttachmentList.SelectedItem is not Attachment attachment)
        {
            MessageBox.Show(this, "Select an attachment to remove.", "Chronolog",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _list.Remove(attachment);
        if (_addedThisSession.Contains(attachment))
        {
            // Never committed to the entry list — delete file now
            _addedThisSession.Remove(attachment);
            _attachments.Delete(_entry.Id, attachment);
        }
        else
        {
            _removedThisSession.Add(attachment);
        }

        RefreshAttachmentHint();
    }

    private void AttachmentList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (AttachmentList.SelectedItem is not Attachment attachment)
            return;

        try
        {
            _attachments.Open(_entry.Id, attachment);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Chronolog",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var body = BodyBox.Text?.TrimEnd() ?? string.Empty;
        var title = TitleBox.Text?.Trim() ?? string.Empty;
        var date = DatePicker.SelectedDate ?? DateTime.Today;

        if (string.IsNullOrWhiteSpace(body) &&
            string.IsNullOrWhiteSpace(title) &&
            _list.Count == 0)
        {
            MessageBox.Show(
                this,
                "Write something or attach a file before saving.",
                "Chronolog",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            BodyBox.Focus();
            return;
        }

        // Apply removals to disk
        foreach (var removed in _removedThisSession)
            _attachments.Delete(_entry.Id, removed);

        _entry.Title = title;
        _entry.Body = body;
        _entry.EntryDate = date.Date;
        _entry.UpdatedAt = DateTime.Now;
        if (_isNew)
            _entry.CreatedAt = DateTime.Now;

        _entry.Attachments = _list.ToList();
        ResultEntry = _entry;
        _committed = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        RollbackUncommitted();
        DialogResult = false;
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_committed)
            RollbackUncommitted();
        base.OnClosing(e);
    }

    private void RollbackUncommitted()
    {
        // Delete files added during this edit session that were never saved
        foreach (var added in _addedThisSession.ToList())
            _attachments.Delete(_entry.Id, added);

        _addedThisSession.Clear();

        // If brand-new entry was cancelled entirely, wipe its folder
        if (_isNew)
            _attachments.DeleteAllForEntry(_entry.Id);
    }
}
