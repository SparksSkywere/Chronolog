using System.Windows;
using Chronolog.Themes;

namespace Chronolog;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Seed numeric style keys (not declared in XAML for TFM portability), then theme.
        Resources["UiFontSize"] = 12.0;
        Resources["JournalFontSize"] = 14.0;
        Resources["ControlMinHeight"] = 26.0;
        ThemeService.Apply(ThemeCatalog.Default);
    }
}
