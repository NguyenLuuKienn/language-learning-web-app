namespace LanguageLearning.Application.DTOs.Sharing;

public class PublicDeckResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid LanguageId { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public int CardsCount { get; set; }
    public int TotalCards => CardsCount;
    public int CloneCount { get; set; } = 0;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
