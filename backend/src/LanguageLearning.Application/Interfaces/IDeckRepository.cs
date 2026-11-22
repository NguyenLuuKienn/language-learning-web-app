
using LanguageLearning.Domain.Entities;
using LanguageLearning.Application.DTOs.Sharing;

namespace LanguageLearning.Application.Interfaces;

public interface IDeckRepository
{
    Task<(List<Deck> Items, int TotalItems)> GetAccessibleDecksAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default,
        Guid? languageId = null,
        string? level = null);

    Task<List<PublicDeckResponse>> GetDeckSummariesAsync(Guid? languageId, string? search, bool includePrivate, CancellationToken cancellationToken = default, string? level = null);

    Task<List<Deck>> GetChangedUserDecksAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null);
    Task<List<Card>> GetChangedUserCardsAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null);

    Task<Deck?> GetByIdAsync(
        Guid deckId,
        CancellationToken cancellationToken = default);

    Task<bool> LanguageExistsAsync(
        Guid languageId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Deck deck,
        CancellationToken cancellationToken = default);

    void Update(Deck deck);

    void Delete(Deck deck);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, (int TotalCards, int NewCards, int DueCards)>>
        GetDeckStatisticsBatchAsync(
            IReadOnlyCollection<Guid> deckIds,
            Guid userId,
            CancellationToken cancellationToken = default);

    Task<(int TotalCards, int NewCards, int DueCards)> GetDeckStatisticsAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
