
using LanguageLearning.Application.DTOs.Decks;

namespace LanguageLearning.Application.Interfaces;

public interface IDeckService
{
    Task<List<DeckResponse>> GetDecksAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DeckResponse?> GetDeckByIdAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DeckResponse> CreateDeckAsync(
        CreateDeckRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DeckResponse?> UpdateDeckAsync(
        Guid deckId,
        UpdateDeckRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDeckAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
