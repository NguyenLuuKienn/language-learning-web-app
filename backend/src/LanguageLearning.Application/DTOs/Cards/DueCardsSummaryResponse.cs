namespace LanguageLearning.Application.DTOs.Cards;

public class DueCardsSummaryResponse
{
    public int DueCount { get; set; }
    public int NewCount { get; set; }
    public List<CardResponse> Cards { get; set; } = new();
}
