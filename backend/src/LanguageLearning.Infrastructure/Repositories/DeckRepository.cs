
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Application.DTOs.Sharing;
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

    public async Task<(List<Deck> Items, int TotalItems)> GetAccessibleDecksAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default,
        Guid? languageId = null,
        string? level = null)
    {
        var query = _context.Decks
            .AsNoTracking()
            .Where(d => !d.IsDeleted && d.OwnerId == userId &&
                (d.Owner.Role == UserRole.Learner || !d.Tags.Any(tag => tag.Tag.StartsWith("course-level:")) ||
                    d.Tags.Any(tag => tag.Tag.StartsWith("vocab-lesson:"))));
        if (languageId.HasValue)
            query = query.Where(d => d.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level))
        {
            var levelTag = "course-level:" + level.ToLower();
            query = query.Where(d => !d.Tags.Any(tag => tag.Tag.StartsWith("course-level:") && tag.Tag.ToLower() != levelTag));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(d => d.Language)
            .Include(d => d.Tags)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<List<PublicDeckResponse>> GetDeckSummariesAsync(Guid? languageId, string? search, bool includePrivate, CancellationToken cancellationToken = default, string? level = null)
    {
        var query = _context.Decks.AsNoTracking().Where(deck => !deck.IsDeleted);
        if (!includePrivate) query = query.Where(deck => deck.IsPublic &&
            (deck.Owner.Role == UserRole.Learner || !deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:")) ||
                deck.Tags.Any(tag => tag.Tag.StartsWith("vocab-lesson:"))));
        if (languageId.HasValue && languageId.Value != Guid.Empty)
            query = query.Where(deck => deck.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level))
        {
            var levelTag = "course-level:" + level.ToLower();
            query = query.Where(deck => deck.Tags.Any(tag => tag.Tag.ToLower() == levelTag));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(deck => deck.Title.ToLower().Contains(term) || deck.Description.ToLower().Contains(term));
        }

        return query.OrderByDescending(deck => deck.CreatedAt)
            .Select(deck => new PublicDeckResponse
            {
                Id = deck.Id,
                Title = deck.Title,
                Description = deck.Description,
                LanguageId = deck.LanguageId,
                LanguageName = deck.Language.Name,
                OwnerDisplayName = deck.Owner.DisplayName,
                CardsCount = deck.Cards.Count,
                CloneCount = _context.Decks.Count(copy => copy.SourceDeckId == deck.Id && !copy.IsDeleted),
                Tags = deck.Tags.Select(tag => tag.Tag).ToList(),
                CreatedAt = deck.CreatedAt
            }).ToListAsync(cancellationToken);
    }

    public Task<List<Deck>> GetChangedUserDecksAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null)
    {
        var query = _context.Decks.AsNoTracking()
            .Where(deck => deck.OwnerId == userId && !deck.IsDeleted && (deck.UpdatedAt ?? deck.CreatedAt) > sinceUtc);
        if (languageId.HasValue) query = query.Where(deck => deck.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level))
        {
            var levelTag = "course-level:" + level.ToLower();
            query = query.Where(deck => !deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:") && tag.Tag.ToLower() != levelTag));
        }
        return query
            .Include(deck => deck.Tags)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Card>> GetChangedUserCardsAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null)
    {
        var query = _context.Cards.AsNoTracking()
            .Where(card => card.Deck.OwnerId == userId && !card.Deck.IsDeleted && (card.UpdatedAt ?? card.CreatedAt) > sinceUtc);
        if (languageId.HasValue) query = query.Where(card => card.Deck.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level))
        {
            var levelTag = "course-level:" + level.ToLower();
            query = query.Where(card => !card.Deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:") && tag.Tag.ToLower() != levelTag));
        }
        return query
            .ToListAsync(cancellationToken);
    }

    public async Task<Deck?> GetByIdAsync(
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Decks
            .Include(d => d.Language)
            .Include(d => d.Owner)
            .Include(d => d.Tags)
            .FirstOrDefaultAsync(
                d => d.Id == deckId && !d.IsDeleted,
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
        deck.IsDeleted = true;
        deck.DeletedAt = DateTime.UtcNow;
        deck.Version++;

        var entry = _context.Entry(deck);

        entry.Property(d => d.IsDeleted).IsModified = true;
        entry.Property(d => d.DeletedAt).IsModified = true;
        entry.Property(d => d.Version).IsModified = true;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, (int TotalCards, int NewCards, int DueCards)>>
        GetDeckStatisticsBatchAsync(
            IReadOnlyCollection<Guid> deckIds,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        if (deckIds.Count == 0)
        {
            return new Dictionary<Guid, (int, int, int)>();
        }

        var now = DateTime.UtcNow;
        var ids = deckIds.Distinct().ToArray();

        var statistics = await _context.Cards
            .AsNoTracking()
            .Where(card => ids.Contains(card.DeckId))
            .GroupBy(card => card.DeckId)
            .Select(group => new
            {
                DeckId = group.Key,
                TotalCards = group.Count(),
                NewCards = group.Count(card =>
                    !card.Schedules.Any(schedule =>
                        schedule.UserId == userId)),
                DueCards = group.Count(card =>
                    card.Schedules.Any(schedule =>
                        schedule.UserId == userId &&
                        schedule.DueAt <= now))
            })
            .ToListAsync(cancellationToken);

        return statistics.ToDictionary(
            item => item.DeckId,
            item => (
                item.TotalCards,
                item.NewCards,
                item.DueCards
            ));
    }

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
