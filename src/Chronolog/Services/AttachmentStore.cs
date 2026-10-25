using System.IO;
using Chronolog.Models;

namespace Chronolog.Services;

/// <summary>
/// Stores attachment binaries on disk next to the journal metadata.
/// Layout: %LocalAppData%\Chronolog\attachments\{entryId}\{attachmentId}_{safeName}
/// </summary>
public class AttachmentStore
{
    private readonly string _root;

    public AttachmentStore(string? dataDirectory = null)
    {
        var appData = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Chronolog");

        _root = Path.Combine(appData, "attachments");
        Directory.CreateDirectory(_root);
    }

    public string RootPath => _root;

    public string GetEntryDirectory(Guid entryId) =>
        Path.Combine(_root, entryId.ToString("N"));

    public string GetFullPath(Guid entryId, Attachment attachment) =>
        Path.Combine(GetEntryDirectory(entryId), attachment.StoredName);

    public Attachment Import(Guid entryId, string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("File not found.", sourcePath);

        var info = new FileInfo(sourcePath);
        var id = Guid.NewGuid();
        var originalName = Path.GetFileName(sourcePath);
        var safeName = SanitizeFileName(originalName);
        var storedName = $"{id:N}_{safeName}";

        var dir = GetEntryDirectory(entryId);
        Directory.CreateDirectory(dir);

        var dest = Path.Combine(dir, storedName);
        File.Copy(sourcePath, dest, overwrite: false);

        return new Attachment
        {
            Id = id,
            FileName = originalName,
            StoredName = storedName,
            ContentType = Attachment.GuessContentType(originalName),
            SizeBytes = info.Length,
            AddedAt = DateTime.Now
        };
    }

    public void Delete(Guid entryId, Attachment attachment)
    {
        try
        {
            var path = GetFullPath(entryId, attachment);
            if (File.Exists(path))
                File.Delete(path);

            var dir = GetEntryDirectory(entryId);
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    public void DeleteAllForEntry(Guid entryId)
    {
        try
        {
            var dir = GetEntryDirectory(entryId);
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    public bool Exists(Guid entryId, Attachment attachment) =>
        File.Exists(GetFullPath(entryId, attachment));

    public void Open(Guid entryId, Attachment attachment)
    {
        var path = GetFullPath(entryId, attachment);
        if (!File.Exists(path))
            throw new FileNotFoundException("Attachment file is missing.", path);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    public void RevealInExplorer(Guid entryId, Attachment attachment)
    {
        var path = GetFullPath(entryId, attachment);
        if (!File.Exists(path))
            throw new FileNotFoundException("Attachment file is missing.", path);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{path}\"",
            UseShellExecute = true
        });
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "file";

        // Keep names reasonable
        if (cleaned.Length > 120)
        {
            var ext = Path.GetExtension(cleaned);
            var stem = Path.GetFileNameWithoutExtension(cleaned);
            cleaned = stem.Substring(0, Math.Min(stem.Length, 100)) + ext;
        }

        return cleaned;
    }
}
