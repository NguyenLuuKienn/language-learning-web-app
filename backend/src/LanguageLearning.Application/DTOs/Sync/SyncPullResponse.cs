using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.DTOs.Decks;

namespace LanguageLearning.Application.DTOs.Sync;

public class SyncPullResponse
{
    public DateTime ServerTimestamp { get; set; } = DateTime.UtcNow;
    public DateTime NewCursor => ServerTimestamp;
    public List<DeckResponse> UpdatedDecks { get; set; } = new();
    public List<CardResponse> UpdatedCards { get; set; } = new();
}
