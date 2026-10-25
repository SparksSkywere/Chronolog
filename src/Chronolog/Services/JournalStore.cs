using System.IO;
using System.Text.Json;
using Chronolog.Models;

namespace Chronolog.Services;

/// <summary>
/// Persists journal metadata as JSON and keeps attachment binaries on disk.
///
/// Library layout (any chosen folder):
///   {DataDirectory}\
///     journals.json
///     attachments\{entryId}\{attachmentId}_{filename}
///
/// The path to DataDirectory is stored machine-locally in appsettings.json so
/// multiple PCs can share one library on OneDrive / network / USB.
/// </summary>
public class JournalStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private string _dataDirectory = string.Empty;
    private string _dataPath = string.Empty;

    public JournalStore(string dataDirectory)
    {
        SetDataDirectory(dataDirectory);
    }

    public string DataPath => _dataPath;
    public string DataDirectory => _dataDirectory;
    public AttachmentStore Attachments { get; private set; } = null!;

    public void SetDataDirectory(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
            throw new ArgumentException("Data directory is required.", nameof(dataDirectory));

        _dataDirectory = Path.GetFullPath(dataDirectory);
        Directory.CreateDirectory(_dataDirectory);
        _dataPath = Path.Combine(_dataDirectory, "journals.json");
        Attachments = new AttachmentStore(_dataDirectory);
    }

    public bool LibraryExists => File.Exists(_dataPath);

    public ChronologData Load()
    {
        if (!File.Exists(_dataPath))
            return CreateDefaultData();

        try
        {
            var json = File.ReadAllText(_dataPath);
            var data = JsonSerializer.Deserialize<ChronologData>(json, JsonOptions);
            if (data is null || data.Journals.Count == 0)
                return CreateDefaultData();

            foreach (var journal in data.Journals)
            {
                foreach (var entry in journal.Entries)
                    entry.Attachments ??= [];

                journal.Entries = journal.Entries
                    .OrderByDescending(e => e.EntryDate)
                    .ThenByDescending(e => e.CreatedAt)
                    .ToList();
            }

            return data;
        }
        catch
        {
            return CreateDefaultData();
        }
    }

    public void Save(ChronologData data)
    {
        Directory.CreateDirectory(_dataDirectory);

        var temp = _dataPath + ".tmp";
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(temp, json);
        File.Copy(temp, _dataPath, overwrite: true);
        try { File.Delete(temp); } catch { /* ignore */ }
    }

    public void DeleteEntryAttachments(JournalEntry entry)
    {
        Attachments.DeleteAllForEntry(entry.Id);
        entry.Attachments.Clear();
    }

    /// <summary>
    /// Copies journals.json and the attachments folder into another library root.
    /// </summary>
    public static void CopyLibrary(string sourceDir, string destDir)
    {
        sourceDir = Path.GetFullPath(sourceDir);
        destDir = Path.GetFullPath(destDir);

        if (string.Equals(sourceDir, destDir, StringComparison.OrdinalIgnoreCase))
            return;

        Directory.CreateDirectory(destDir);

        var srcJournals = Path.Combine(sourceDir, "journals.json");
        if (File.Exists(srcJournals))
        {
            File.Copy(srcJournals, Path.Combine(destDir, "journals.json"), overwrite: true);
        }

        var srcAttachments = Path.Combine(sourceDir, "attachments");
        var destAttachments = Path.Combine(destDir, "attachments");
        if (Directory.Exists(srcAttachments))
            CopyDirectory(srcAttachments, destAttachments);
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
        {
            var name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(dest, name), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(dir);
            CopyDirectory(dir, Path.Combine(dest, name));
        }
    }

    private static ChronologData CreateDefaultData()
    {
        var journal = new Journal
        {
            Name = "Personal Journal",
            Owner = Environment.UserName
        };

        journal.Entries.Add(new JournalEntry
        {
            Title = "First Entry",
            EntryDate = DateTime.Today,
            Body =
                "Welcome to Chronolog.\n\n" +
                "This is your personal journal — a quiet place to record the days as they pass.\n\n" +
                "Use File → Storage Location… to put your library on OneDrive, a network drive, or a USB stick so every computer can share the same entries.\n\n" +
                "Use File → New Journal to create additional journals, or click Write Journal to begin.\n\n" +
                "You can attach images and files to any entry."
        });

        return new ChronologData
        {
            Journals = [journal],
            LastSelectedJournalId = journal.Id
        };
    }
}
