using LanguageLearning.Application.DTOs.Cards;

namespace LanguageLearning.Application.Interfaces;

public interface ICardService
{
    Task<List<CardResponse>> GetCardsByDeckIdAsync(Guid deckId, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<CardResponse> GetCardByIdAsync(Guid cardId, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<CardResponse> CreateCardAsync(Guid deckId, CreateCardRequest request, Guid userId, CancellationToken cancellationToken = default, Guid? clientCardId = null);
    Task<List<CardResponse>> CreateBatchCardsAsync(Guid deckId, BatchCreateCardsRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<CardResponse> UpdateCardAsync(Guid cardId, UpdateCardRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteCardAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default);
}
