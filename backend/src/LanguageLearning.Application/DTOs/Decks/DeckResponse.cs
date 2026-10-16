
namespace LanguageLearning.Application.DTOs.Decks;

public class DeckResponse
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public Guid LanguageId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsPublic { get; set; }

    public int Version { get; set; }

    public List<string> Tags { get; set; } = new();

    public int TotalCards { get; set; }

    public int NewCards { get; set; }

    public int DueCards { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
