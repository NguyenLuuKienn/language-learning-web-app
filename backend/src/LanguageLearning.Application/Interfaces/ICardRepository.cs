using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface ICardRepository
{
    Task<List<Card>> GetByDeckIdAsync(Guid deckId, CancellationToken cancellationToken = default);
    Task<List<Card>> GetSampleByDeckIdAsync(Guid deckId, int limit, CancellationToken cancellationToken = default);
    Task<int> CountByDeckIdAsync(Guid deckId, CancellationToken cancellationToken = default);
    Task<Card?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Card card, CancellationToken cancellationToken = default);
    Task AddBatchAsync(IEnumerable<Card> cards, CancellationToken cancellationToken = default);
    Task UpdateAsync(Card card, CancellationToken cancellationToken = default);
    Task DeleteAsync(Card card, CancellationToken cancellationToken = default);
}
