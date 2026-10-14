
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IDeckRepository
{
    Task<List<Deck>> GetAccessibleDecksAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

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

    
    Task<(int TotalCards, int NewCards, int DueCards)> GetDeckStatisticsAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default);

}
