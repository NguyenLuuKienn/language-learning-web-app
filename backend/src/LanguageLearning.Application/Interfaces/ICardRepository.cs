
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface ICardRepository
{
    Task<List<Card>> GetByDeckIdAsync(
        Guid deckId,
        CancellationToken cancellationToken = default);

    Task<Card?> GetByIdAsync(
        Guid deckId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Card card,
        CancellationToken cancellationToken = default);

    void Update(Card card);

    Task<bool> HasReviewEventsAsync(
    Guid cardId,
    CancellationToken cancellationToken = default);

    void Delete(Card card);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
