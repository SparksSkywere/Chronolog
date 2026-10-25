using System.IO;
using System.Text.Json;
using Chronolog.Models;

namespace Chronolog.Services;

public class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;
    private readonly string _defaultDataDirectory;

    public AppSettingsStore()
    {
        var localRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Chronolog");

        Directory.CreateDirectory(localRoot);
        _settingsPath = Path.Combine(localRoot, "appsettings.json");
        _defaultDataDirectory = Path.Combine(localRoot, "Library");
    }

    public string SettingsPath => _settingsPath;
    public string DefaultDataDirectory => _defaultDataDirectory;
    public string LocalConfigDirectory => Path.GetDirectoryName(_settingsPath)!;

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            // Migrate older installs that wrote journals.json into the Chronolog root
            var legacyDir = LocalConfigDirectory;
            var legacyJournals = Path.Combine(legacyDir, "journals.json");
            if (File.Exists(legacyJournals))
            {
                return new AppSettings { DataDirectory = legacyDir };
            }

            return new AppSettings { DataDirectory = _defaultDataDirectory };
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                           ?? new AppSettings();

            if (string.IsNullOrWhiteSpace(settings.DataDirectory))
                settings.DataDirectory = _defaultDataDirectory;

            return settings;
        }
        catch
        {
            return new AppSettings { DataDirectory = _defaultDataDirectory };
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(LocalConfigDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var temp = _settingsPath + ".tmp";
        File.WriteAllText(temp, json);
        File.Copy(temp, _settingsPath, overwrite: true);
        File.Delete(temp);
    }
}
