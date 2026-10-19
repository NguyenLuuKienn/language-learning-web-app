
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

    public async Task<List<Card>> GetByDeckIdAsync(
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Cards
            .AsNoTracking()
            .Where(card => card.DeckId == deckId)
            .OrderBy(card => card.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Card?> GetByIdAsync(
        Guid deckId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Cards
            .FirstOrDefaultAsync(
                card => card.DeckId == deckId &&
                        card.Id == cardId,
                cancellationToken);
    }

    public async Task AddAsync(
        Card card,
        CancellationToken cancellationToken = default)
    {
        await _context.Cards.AddAsync(
            card,
            cancellationToken);
    }

    public void Update(Card card)
    {
        _context.Cards.Update(card);
    }

    public void Delete(Card card)
    {
        _context.Cards.Remove(card);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
