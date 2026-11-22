namespace LanguageLearning.Application.DTOs.Sharing;

public class CloneDeckResponse
{
    public Guid ClonedDeckId { get; set; }
    public Guid SourceDeckId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ClonedCardsCount { get; set; }
}
