
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IDeckRepository
{

    Task<(List<Deck> Items, int TotalItems)> GetAccessibleDecksAsync(
        Guid userId,
        int page,
        int pageSize,
        bool onlyMine = false,
        CancellationToken cancellationToken = default,
        string? search = null);

    Task<List<Deck>> GetAllAsync(
        CancellationToken cancellationToken = default);

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
