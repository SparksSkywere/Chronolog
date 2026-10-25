namespace Chronolog.Models;

/// <summary>
/// Machine-local pointer to where journal data lives.
/// Stored under %LocalAppData%\Chronolog so each PC can point at a shared folder
/// (OneDrive, network drive, USB, etc.).
/// </summary>
public class AppSettings
{
    public int Version { get; set; } = 1;

    /// <summary>Absolute path to the journal library folder (journals.json + attachments).</summary>
    public string? DataDirectory { get; set; }
}
