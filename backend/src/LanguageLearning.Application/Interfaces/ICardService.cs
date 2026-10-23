
using LanguageLearning.Application.DTOs.Cards;

namespace LanguageLearning.Application.Interfaces;

public interface ICardService
{
    Task<List<CardResponse>> GetCardsAsync(
        Guid deckId,
        CancellationToken cancellationToken = default);

    Task<CardResponse> GetCardByIdAsync(
        Guid deckId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<CardResponse> CreateCardAsync(
        Guid deckId,
        CreateCardRequest request,
        CancellationToken cancellationToken = default);

    Task<CardResponse> UpdateCardAsync(
        Guid deckId,
        Guid cardId,
        UpdateCardRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCardAsync(
        Guid deckId,
        Guid cardId,
        CancellationToken cancellationToken = default);
}
