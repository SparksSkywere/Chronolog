using System.Windows;
using System.Windows.Media;

namespace Chronolog.Themes;

public static class ThemeService
{
    public static ThemeDefinition Current { get; private set; } = ThemeCatalog.Default;

    public static event EventHandler? ThemeChanged;

    public static void Apply(string? themeId)
    {
        var theme = ThemeCatalog.Find(themeId) ?? ThemeCatalog.Default;
        Apply(theme);
    }

    public static void Apply(ThemeDefinition theme)
    {
        Current = theme;
        var app = Application.Current;
        if (app is null)
            return;

        var r = app.Resources;

        SetBrush(r, "FaceBrush", theme.Face);
        SetBrush(r, "WindowBrush", theme.Window);
        SetBrush(r, "ParchmentBrush", theme.Parchment);
        SetBrush(r, "InkBrush", theme.Ink);
        SetBrush(r, "HighlightBrush", theme.Highlight);
        SetBrush(r, "HighlightTextBrush", theme.HighlightText);
        SetBrush(r, "ShadowBrush", theme.Shadow);
        SetBrush(r, "DarkShadowBrush", theme.DarkShadow);
        SetBrush(r, "LightBrush", theme.Light);
        SetBrush(r, "AccentBrush", theme.Accent);
        SetBrush(r, "AccentBorderBrush", theme.AccentBorder);
        SetBrush(r, "SidebarBrush", theme.Sidebar);
        SetBrush(r, "DisabledTextBrush", theme.DisabledText);
        SetBrush(r, "MenuBarBrush", theme.MenuBar);
        SetBrush(r, "MenuBarTextBrush", theme.MenuBarText);
        SetBrush(r, "HoverBrush", theme.Hover);
        SetBrush(r, "TabInactiveBrush", theme.TabInactive);
        SetBrush(r, "PrimaryButtonBrush", theme.PrimaryButton);
        SetBrush(r, "BorderBrushKey", theme.Border);
        SetBrush(r, "StatusBarBrush", theme.StatusBar);
        SetBrush(r, "PlaceholderBrush", theme.Placeholder);
        SetBrush(r, "MetaTextBrush", theme.MetaText);

        // Primary button text: white on modern accent buttons, ink on classic gray ones
        var primaryText = theme.Chrome is ThemeChrome.Flat or ThemeChrome.Fluent
            ? theme.HighlightText
            : theme.Ink;
        // Win11 dark primary is light cyan — use dark text
        if (theme.Id == "win11-dark")
            primaryText = theme.HighlightText;
        if (theme.Id is "win8" or "win10" or "win11")
            primaryText = Colors.White;
        SetBrush(r, "PrimaryButtonTextBrush", primaryText);

        r["UiFont"] = new FontFamily(theme.UiFont);
        r["JournalFont"] = new FontFamily(theme.JournalFont);
        r["MonoFont"] = new FontFamily("Consolas, Courier New, monospace");

        r["CornerRadius"] = new CornerRadius(theme.CornerRadius);
        r["ButtonCornerRadius"] = new CornerRadius(theme.ButtonCornerRadius);
        r["UiFontSize"] = theme.UiFontSize;
        r["JournalFontSize"] = theme.JournalFontSize;
        r["ControlMinHeight"] = theme.ControlHeight;
        r["ChromeStyle"] = theme.Chrome.ToString();

        // Classic chrome uses nested 3D bevels; modern uses single flat/rounded border
        r["UseClassicBevel"] = theme.Chrome is ThemeChrome.Classic or ThemeChrome.Luna;
        r["UseSoftChrome"] = theme.Chrome is ThemeChrome.Aero or ThemeChrome.Fluent;

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void SetBrush(ResourceDictionary r, string key, Color color)
    {
        if (r[key] is SolidColorBrush existing && !existing.IsFrozen)
        {
            existing.Color = color;
            return;
        }

        var brush = new SolidColorBrush(color);
        brush.Freeze();
        r[key] = brush;
    }
}
