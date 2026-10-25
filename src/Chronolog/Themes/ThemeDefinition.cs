using System.Windows.Media;

namespace Chronolog.Themes;

public enum ThemeChrome
{
    Classic,  // Win95 / 98 / 2000 — hard 3D bevels
    Luna,     // WinXP — soft blue gradients
    Aero,     // Vista / 7 — glass-ish light chrome
    Flat,     // Win8 / 10 — flat borders
    Fluent    // Win11 — rounded, soft acrylic-ish
}

public sealed class ThemeDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Era { get; set; } = "";
    public ThemeChrome Chrome { get; set; }

    public Color Face { get; set; }
    public Color Window { get; set; }
    public Color Parchment { get; set; }
    public Color Ink { get; set; }
    public Color Highlight { get; set; }
    public Color HighlightText { get; set; }
    public Color Shadow { get; set; }
    public Color DarkShadow { get; set; }
    public Color Light { get; set; }
    public Color Accent { get; set; }
    public Color AccentBorder { get; set; }
    public Color Sidebar { get; set; }
    public Color DisabledText { get; set; }
    public Color MenuBar { get; set; }
    public Color MenuBarText { get; set; }
    public Color Hover { get; set; }
    public Color TabInactive { get; set; }
    public Color PrimaryButton { get; set; }
    public Color Border { get; set; }
    public Color StatusBar { get; set; }
    public Color Placeholder { get; set; }
    public Color MetaText { get; set; }

    public string UiFont { get; set; } = "";
    public string JournalFont { get; set; } = "";

    public double CornerRadius { get; set; }
    public double ButtonCornerRadius { get; set; }
    public double ControlHeight { get; set; } = 26;
    public double UiFontSize { get; set; } = 12;
    public double JournalFontSize { get; set; } = 14;
}
