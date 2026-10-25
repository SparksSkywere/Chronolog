using System.IO;
using System.Windows;
using Chronolog.Services;

namespace Chronolog.Views;

public enum StorageChangeMode
{
    SwitchOnly,
    Copy,
    Move
}

public partial class StorageLocationWindow : Window
{
    private readonly string _defaultDirectory;
    private readonly string _currentDirectory;

    public string? SelectedDirectory { get; private set; }
    public StorageChangeMode ChangeMode { get; private set; } = StorageChangeMode.SwitchOnly;

    public StorageLocationWindow(string currentDirectory, string defaultDirectory)
    {
        InitializeComponent();
        _currentDirectory = Path.GetFullPath(currentDirectory);
        _defaultDirectory = Path.GetFullPath(defaultDirectory);
        PathBox.Text = _currentDirectory;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
#if NETFRAMEWORK
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Choose a folder for your Chronolog journal library",
            ShowNewFolderButton = true
        };

        if (Directory.Exists(PathBox.Text))
            dialog.SelectedPath = PathBox.Text;
        else if (Directory.Exists(_currentDirectory))
            dialog.SelectedPath = _currentDirectory;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            PathBox.Text = dialog.SelectedPath;
        }
#else
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose a folder for your Chronolog journal library",
            Multiselect = false
        };

        if (Directory.Exists(PathBox.Text))
            dialog.InitialDirectory = PathBox.Text;
        else if (Directory.Exists(_currentDirectory))
            dialog.InitialDirectory = _currentDirectory;

        if (dialog.ShowDialog(this) == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            PathBox.Text = dialog.FolderName;
#endif
    }

    private void Default_Click(object sender, RoutedEventArgs e)
    {
        PathBox.Text = _defaultDirectory;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(this, "Choose a folder.", "Chronolog",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(path);

            var probe = Path.Combine(path, ".chronolog-write-test");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Cannot use that folder:\n{ex.Message}",
                "Chronolog",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        SelectedDirectory = path;
        ChangeMode = OptionCopy.IsChecked == true
            ? StorageChangeMode.Copy
            : OptionMove.IsChecked == true
                ? StorageChangeMode.Move
                : StorageChangeMode.SwitchOnly;

        if (string.Equals(path, _currentDirectory, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "That is already the current library folder.", "Chronolog",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = false;
            Close();
            return;
        }

        var targetJournals = Path.Combine(path, "journals.json");
        if (File.Exists(targetJournals) && ChangeMode is StorageChangeMode.Copy or StorageChangeMode.Move)
        {
            var overwrite = MessageBox.Show(this,
                "The destination already contains a Chronolog library.\n\n" +
                "Copy/Move will overwrite journals.json and merge attachment files with the same names.\n\nContinue?",
                "Storage Location",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (overwrite != MessageBoxResult.Yes)
                return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
