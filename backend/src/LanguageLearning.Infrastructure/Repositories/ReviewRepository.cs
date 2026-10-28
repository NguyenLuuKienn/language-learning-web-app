
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;

    public ReviewRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CardSchedule>> GetDueSchedulesAsync(
        Guid userId,
        Guid deckId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.CardSchedules
            .AsNoTracking()
            .Include(schedule => schedule.Card)
            .Where(schedule =>
                schedule.UserId == userId &&
                schedule.Card.DeckId == deckId &&
                !schedule.Card.Deck.IsDeleted &&
                schedule.DueAt <= nowUtc)
            .OrderBy(schedule => schedule.DueAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CardSchedule?> GetScheduleAsync(
        Guid userId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CardSchedules
            .FirstOrDefaultAsync(
                schedule =>
                    schedule.UserId == userId &&
                    schedule.CardId == cardId,
                cancellationToken);
    }

    public async Task AddScheduleAsync(
        CardSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        await _context.CardSchedules.AddAsync(
            schedule,
            cancellationToken);
    }

    public async Task AddReviewEventAsync(
        ReviewEvent reviewEvent,
        CancellationToken cancellationToken = default)
    {
        await _context.ReviewEvents.AddAsync(
            reviewEvent,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
