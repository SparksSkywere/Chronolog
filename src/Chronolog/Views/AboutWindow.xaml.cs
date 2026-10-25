using System.Reflection;
using System.Windows;

namespace Chronolog.Views;

public partial class AboutWindow : Window
{
    public AboutWindow(string? libraryPath = null)
    {
        InitializeComponent();

        Title = $"About {AppEdition.ProductName}";
        ProductNameText.Text = AppEdition.ProductName;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null
            ? $"Version 1.0 · {AppEdition.RuntimeLabel}"
            : $"Version {version.Major}.{version.Minor}.{version.Build} · {AppEdition.RuntimeLabel}";

        EditionOsText.Text = $"Supports: {AppEdition.SupportedOsLabel}";
        SuiteLineText.Text = $"{AppEdition.ProductName} — Skywere Industries";

        AboutBodyText.Text =
            "Chronolog is a personal offline journal for keeping the days — entries, attachments, " +
            "and multiple volumes on one machine or shared across your computers.\n\n" +
            "Point File → Storage Location… at a cloud or network folder to add and edit the same library from any PC.\n\n" +
            $"This build is {AppEdition.ProductName} ({AppEdition.ShortName}). " +
            "Use Chronolog Modern on Windows 10/11, or Chronolog Classic on Windows 7 SP1 / 8 / 8.1.";

        LibraryPathText.Text = string.IsNullOrWhiteSpace(libraryPath)
            ? "(not set)"
            : libraryPath;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
