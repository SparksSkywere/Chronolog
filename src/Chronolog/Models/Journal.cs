namespace Chronolog.Models;

public class Journal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "My Journal";
    public string Owner { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<JournalEntry> Entries { get; set; } = [];

    public string TabTitle => string.IsNullOrWhiteSpace(Owner)
        ? Name
        : $"{Owner}'s Journal";
}
