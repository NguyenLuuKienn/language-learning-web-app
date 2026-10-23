
namespace LanguageLearning.Application.DTOs.Cards;

public class CardResponse
{
    public Guid Id { get; set; }

    public Guid DeckId { get; set; }

    public string Front { get; set; } = string.Empty;

    public string Back { get; set; } = string.Empty;

    public int Version { get; set; }
}
