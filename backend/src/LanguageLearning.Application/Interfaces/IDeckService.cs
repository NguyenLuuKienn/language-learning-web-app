
using LanguageLearning.Application.Common;
using LanguageLearning.Application.DTOs.Decks;

namespace LanguageLearning.Application.Interfaces;

public interface IDeckService
{
    Task<PagedResponse<DeckResponse>> GetDecksAsync(
        PaginationParams pagination,
        bool onlyMine = false,
        CancellationToken cancellationToken = default,
        string? search = null);
    Task<DeckResponse> GetDeckByIdAsync(
        Guid deckId,
        CancellationToken cancellationToken = default);

    Task<DeckResponse> CreateDeckAsync(
        CreateDeckRequest request,
        CancellationToken cancellationToken = default);

    Task<DeckResponse> UpdateDeckAsync(
        Guid deckId,
        UpdateDeckRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteDeckAsync(
        Guid deckId,
        CancellationToken cancellationToken = default);
}
