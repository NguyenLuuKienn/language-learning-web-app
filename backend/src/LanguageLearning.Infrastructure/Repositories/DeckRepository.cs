
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class DeckRepository : IDeckRepository
{
    private readonly AppDbContext _context;

    public DeckRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Deck>> GetAccessibleDecksAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Decks
            .AsNoTracking()
            .Where(d => d.OwnerId == userId || d.IsPublic)
            .Include(d => d.Tags)
            .ToListAsync(cancellationToken);
    }   

    public async Task<List<Deck>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Decks
            .AsNoTracking()
            .Include(d => d.Tags)
            .Include(d => d.Cards)
                .ThenInclude(c => c.Schedules)
            .ToListAsync(cancellationToken);
    }

    public async Task<Deck?> GetByIdAsync(
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Decks
            .Include(d => d.Tags)
            .Include(d => d.Cards)
                .ThenInclude(c => c.Schedules)
            .FirstOrDefaultAsync(
                d => d.Id == deckId,
                cancellationToken);
    }

    public async Task<bool> LanguageExistsAsync(
        Guid languageId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Languages
            .AnyAsync(
                l => l.Id == languageId,
                cancellationToken);
    }

    public async Task AddAsync(
        Deck deck,
        CancellationToken cancellationToken = default)
    {
        await _context.Decks.AddAsync(
            deck,
            cancellationToken);
    }

    public void Update(Deck deck)
    {
        _context.Decks.Update(deck);
    }

    public void Delete(Deck deck)
    {
        _context.Decks.Remove(deck);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    // Get card statistics for a deck and user
    public async Task<(int TotalCards, int NewCards, int DueCards)>
        GetDeckStatisticsAsync(
            Guid deckId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var totalCards = await _context.Cards
            .CountAsync(
                card => card.DeckId == deckId,
                cancellationToken);

        var newCards = await _context.Cards
            .CountAsync(
                card => card.DeckId == deckId &&
                    !card.Schedules.Any(s => s.UserId == userId),
                cancellationToken);

        var dueCards = await _context.Cards
            .CountAsync(
                card => card.DeckId == deckId &&
                    card.Schedules.Any(s =>
                        s.UserId == userId && s.DueAt <= now),
                cancellationToken);

        return (totalCards, newCards, dueCards);
    }

}
