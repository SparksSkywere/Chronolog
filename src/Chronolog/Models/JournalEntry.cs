namespace Chronolog.Models;

public class JournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; } = DateTime.Today;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<Attachment> Attachments { get; set; } = [];

    public string DateLabel => EntryDate.ToString("MM-dd-yyyy");

    public string DisplayTitle
    {
        get
        {
            var baseTitle = string.IsNullOrWhiteSpace(Title)
                ? DateLabel
                : $"{DateLabel} — {Title}";

            return Attachments.Count > 0
                ? $"{baseTitle}  📎{Attachments.Count}"
                : baseTitle;
        }
    }

    public bool HasAttachments => Attachments.Count > 0;
}
