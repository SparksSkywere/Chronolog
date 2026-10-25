namespace Chronolog.Models;

public class ChronologData
{
    public int Version { get; set; } = 1;
    public List<Journal> Journals { get; set; } = [];
    public Guid? LastSelectedJournalId { get; set; }
    public string ThemeId { get; set; } = "win2000";
}
