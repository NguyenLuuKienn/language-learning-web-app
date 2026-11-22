using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class CardRepository : ICardRepository
{
    private readonly AppDbContext _context;

    public CardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Card>> GetByDeckIdAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        return await _context.Cards
            .Where(c => c.DeckId == deckId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Card>> GetSampleByDeckIdAsync(Guid deckId, int limit, CancellationToken cancellationToken = default) =>
        _context.Cards.AsNoTracking().Where(card => card.DeckId == deckId)
            .OrderBy(card => card.CreatedAt).Take(limit).ToListAsync(cancellationToken);

    public Task<int> CountByDeckIdAsync(Guid deckId, CancellationToken cancellationToken = default) =>
        _context.Cards.AsNoTracking().CountAsync(card => card.DeckId == deckId, cancellationToken);

    public async Task<Card?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Cards
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(Card card, CancellationToken cancellationToken = default)
    {
        await _context.Cards.AddAsync(card, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddBatchAsync(IEnumerable<Card> cards, CancellationToken cancellationToken = default)
    {
        await _context.Cards.AddRangeAsync(cards, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Card card, CancellationToken cancellationToken = default)
    {
        _context.Cards.Update(card);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Card card, CancellationToken cancellationToken = default)
    {
        _context.Cards.Remove(card);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
