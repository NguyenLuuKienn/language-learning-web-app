using LanguageLearning.Application.DTOs.Cards;

namespace LanguageLearning.Application.DTOs.Sharing;

public class PublicDeckPreviewResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public int TotalCards { get; set; }
    public List<CardResponse> SampleCards { get; set; } = new();
}
