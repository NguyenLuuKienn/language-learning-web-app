namespace LanguageLearning.Application.DTOs.Cards;

public class CardResponse
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public string Phonetic { get; set; } = string.Empty;
    public string PartOfSpeech { get; set; } = string.Empty;
    public List<string> Meanings { get; set; } = new();
    public string Example { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public CardScheduleResponse? Schedule { get; set; }
}
