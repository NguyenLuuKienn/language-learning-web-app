using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;

    public ReviewRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CardSchedule?> GetScheduleAsync(Guid userId, Guid cardId, CancellationToken cancellationToken = default)
    {
        return await _context.CardSchedules
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CardId == cardId, cancellationToken);
    }

    public async Task<List<CardSchedule>> GetSchedulesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CardSchedules
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CardSchedule>> GetSchedulesForCardsAsync(Guid userId, IReadOnlyCollection<Guid> cardIds, CancellationToken cancellationToken = default)
    {
        if (cardIds.Count == 0) return new List<CardSchedule>();
        return await _context.CardSchedules.AsNoTracking()
            .Where(schedule => schedule.UserId == userId && cardIds.Contains(schedule.CardId))
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertScheduleAsync(CardSchedule schedule, CancellationToken cancellationToken = default)
    {
        var existing = await _context.CardSchedules
            .FirstOrDefaultAsync(s => s.UserId == schedule.UserId && s.CardId == schedule.CardId, cancellationToken);

        if (existing == null)
        {
            await _context.CardSchedules.AddAsync(schedule, cancellationToken);
        }
        else
        {
            existing.DueAt = schedule.DueAt;
            existing.StateJson = schedule.StateJson;
            existing.AlgorithmVersion = schedule.AlgorithmVersion;
            existing.LastReviewedAt = schedule.LastReviewedAt;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Card>> GetDueCardsAsync(Guid userId, Guid? deckId, int limit = 50, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null, bool dueOnly = false, bool newOnly = false, Guid? lessonId = null)
    {
        var now = DateTime.UtcNow;

        var query = _context.Cards
            .AsNoTracking()
            .Where(c => c.Deck.OwnerId == userId && !c.Deck.IsDeleted &&
                (c.Deck.Owner.Role == UserRole.Learner ||
                    !c.Deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:")) ||
                    c.Deck.Tags.Any(tag => tag.Tag.StartsWith("vocab-lesson:"))))
            .AsQueryable();

        if (languageId.HasValue)
            query = query.Where(card => card.Deck.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level))
        {
            var levelTag = "course-level:" + level.ToLower();
            query = query.Where(card => !card.Deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:") && tag.Tag.ToLower() != levelTag));
        }

        if (deckId.HasValue && deckId != Guid.Empty)
        {
            query = query.Where(c => c.DeckId == deckId.Value);
        }
        if (lessonId.HasValue)
            query = query.Where(card => _context.CardLessonSources.Any(source =>
                source.UserId == userId && source.LessonId == lessonId.Value && source.CardId == card.Id));
        query = newOnly
            ? query.Where(c => !_context.CardSchedules.Any(s => s.UserId == userId && s.CardId == c.Id))
            : dueOnly
                ? query.Where(c => _context.CardSchedules.Any(s => s.UserId == userId && s.CardId == c.Id && s.DueAt <= now))
                : query.Where(c => !_context.CardSchedules.Any(s => s.UserId == userId && s.CardId == c.Id && s.DueAt > now));
        var ordered = dueOnly
            ? query.OrderBy(card => _context.CardSchedules
                .Where(schedule => schedule.UserId == userId && schedule.CardId == card.Id)
                .Select(schedule => schedule.DueAt).FirstOrDefault())
            : query.OrderBy(card => card.CreatedAt);
        var dueCards = await ordered.Take(limit).ToListAsync(cancellationToken);

        return dueCards;
    }

    public async Task<ReviewOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null, Guid? deckId = null, Guid? lessonId = null)
    {
        var now = DateTime.UtcNow;
        var cards = _context.Cards.AsNoTracking()
            .Where(card => card.Deck.OwnerId == userId && !card.Deck.IsDeleted &&
                (card.Deck.Owner.Role == UserRole.Learner ||
                    !card.Deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:")) ||
                    card.Deck.Tags.Any(tag => tag.Tag.StartsWith("vocab-lesson:"))));
        if (languageId.HasValue) cards = cards.Where(card => card.Deck.LanguageId == languageId.Value);
        if (deckId.HasValue) cards = cards.Where(card => card.DeckId == deckId.Value);
        if (lessonId.HasValue) cards = cards.Where(card => _context.CardLessonSources.Any(source =>
            source.UserId == userId && source.LessonId == lessonId.Value && source.CardId == card.Id));
        if (!string.IsNullOrWhiteSpace(level))
        {
            var levelTag = "course-level:" + level.ToLowerInvariant();
            cards = cards.Where(card => !card.Deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:") && tag.Tag.ToLower() != levelTag));
        }

        var cardIds = await cards.Select(card => card.Id).ToListAsync(cancellationToken);
        if (cardIds.Count == 0) return new ReviewOverviewResponse();
        var schedules = await _context.CardSchedules.AsNoTracking()
            .Where(schedule => schedule.UserId == userId && cardIds.Contains(schedule.CardId))
            .ToListAsync(cancellationToken);
        var observedDue = await _context.ReviewEvents.AsNoTracking()
            .Where(item => item.UserId == userId && cardIds.Contains(item.CardId) && item.WasDue == true)
            .GroupBy(item => 1)
            .Select(group => new { Total = group.Count(), Recalled = group.Count(item => item.Rating == SrsRating.Good || item.Rating == SrsRating.Easy) })
            .FirstOrDefaultAsync(cancellationToken);
        var learning = 0;
        var reviewing = 0;
        var relearning = 0;
        var forecast = new int[7];
        foreach (var schedule in schedules)
        {
            var state = SrsState.FromJson(schedule.StateJson);
            if (state.Lapses > 0 && state.Repetitions < 2) relearning++;
            else if (state.Repetitions < 2) learning++;
            else reviewing++;
            var day = (schedule.DueAt.Date - now.Date).Days;
            if (day >= 0 && day < forecast.Length) forecast[day]++;
        }
        return new ReviewOverviewResponse
        {
            TotalCount = cardIds.Count,
            UnseenCount = cardIds.Count - schedules.Count,
            DueCount = schedules.Count(schedule => schedule.DueAt <= now),
            LearningCount = learning,
            ReviewCount = reviewing,
            RelearningCount = relearning,
            DueForecast7Days = forecast,
            ObservedDueReviews = observedDue?.Total ?? 0,
            ObservedRecallPercent = observedDue == null ? null : (int)Math.Round(observedDue.Recalled * 100.0 / observedDue.Total),
            NextDueAt = schedules.Where(schedule => schedule.DueAt > now)
                .OrderBy(schedule => schedule.DueAt).Select(schedule => (DateTime?)schedule.DueAt).FirstOrDefault()
        };
    }

    public async Task<List<ReviewLessonSourceResponse>> GetLessonSourcesAsync(Guid userId, Guid? languageId, string? level, CancellationToken cancellationToken = default)
    {
        var sources = _context.CardLessonSources.AsNoTracking()
            .Where(source => source.UserId == userId && !source.Card.Deck.IsDeleted);
        if (languageId.HasValue)
            sources = sources.Where(source => source.Card.Deck.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level))
        {
            var tag = "course-level:" + level.ToLowerInvariant();
            sources = sources.Where(source => !source.Card.Deck.Tags.Any(item =>
                item.Tag.StartsWith("course-level:") && item.Tag.ToLower() != tag));
        }
        var counts = await sources.GroupBy(source => source.LessonId)
            .Select(group => new { LessonId = group.Key, CardCount = group.Count() })
            .ToListAsync(cancellationToken);
        var lessonIds = counts.Select(item => item.LessonId).ToArray();
        var titles = await _context.Lessons.AsNoTracking()
            .Where(lesson => lessonIds.Contains(lesson.Id))
            .Select(lesson => new { lesson.Id, lesson.Title, UnitTitle = lesson.Unit.Title })
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        return counts.Where(item => titles.ContainsKey(item.LessonId))
            .Select(item => new ReviewLessonSourceResponse
            {
                LessonId = item.LessonId,
                LessonTitle = titles[item.LessonId].Title,
                UnitTitle = titles[item.LessonId].UnitTitle,
                CardCount = item.CardCount
            }).OrderBy(item => item.UnitTitle).ThenBy(item => item.LessonTitle).ToList();
    }

    public async Task<ReviewEvent?> GetExistingReviewEventAsync(Guid userId, Guid clientEventId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewEvents
            .FirstOrDefaultAsync(e => e.UserId == userId && e.ClientEventId == clientEventId, cancellationToken);
    }

    public async Task AddReviewEventAsync(ReviewEvent reviewEvent, CancellationToken cancellationToken = default)
    {
        await _context.ReviewEvents.AddAsync(reviewEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CommitReviewAsync(CardSchedule schedule, ReviewEvent reviewEvent, string timezone,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                if (await _context.ReviewEvents.AsNoTracking().AnyAsync(item =>
                    item.UserId == reviewEvent.UserId && item.ClientEventId == reviewEvent.ClientEventId,
                    cancellationToken)) return false;
                var existingSchedule = await _context.CardSchedules.FirstOrDefaultAsync(item =>
                    item.UserId == schedule.UserId && item.CardId == schedule.CardId, cancellationToken);
                if (existingSchedule == null) _context.CardSchedules.Add(schedule);
                else
                {
                    existingSchedule.DueAt = schedule.DueAt;
                    existingSchedule.StateJson = schedule.StateJson;
                    existingSchedule.AlgorithmVersion = schedule.AlgorithmVersion;
                    existingSchedule.LastReviewedAt = schedule.LastReviewedAt;
                    existingSchedule.UpdatedAt = DateTime.UtcNow;
                }
                _context.ReviewEvents.Add(reviewEvent);
                await _context.SaveChangesAsync(cancellationToken);
                await RecordDailyReviewActivityAsync(reviewEvent.UserId, timezone, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            });
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            if (await _context.ReviewEvents.AsNoTracking().AnyAsync(item =>
                item.UserId == reviewEvent.UserId && item.ClientEventId == reviewEvent.ClientEventId,
                cancellationToken)) return false;
            throw;
        }
    }

    public async Task RecordDailyReviewActivityAsync(Guid userId, string timezone, CancellationToken cancellationToken = default)
    {
        TimeZoneInfo tzInfo;
        try
        {
            tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch
        {
            tzInfo = TimeZoneInfo.Utc;
        }

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
        var today = DateOnly.FromDateTime(localNow);

        var stats = await _context.DailyLearningStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.LocalDate == today, cancellationToken);

        var goal = await _context.UserGoals
            .FirstOrDefaultAsync(g => g.UserId == userId, cancellationToken);

        int target = goal?.DailyTarget ?? 10;

        if (stats == null)
        {
            stats = new DailyLearningStats
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LocalDate = today,
                Timezone = timezone,
                ReviewCount = 1,
                LessonsCompleted = 0,
                GoalMet = 1 >= target,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.DailyLearningStats.AddAsync(stats, cancellationToken);
        }
        else
        {
            stats.ReviewCount += 1;
            stats.GoalMet = stats.ReviewCount >= target;
            stats.UpdatedAt = DateTime.UtcNow;
            _context.DailyLearningStats.Update(stats);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ReviewEvent>> GetReviewHistoryAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null)
    {
        var query = _context.ReviewEvents.AsNoTracking().Where(e => e.UserId == userId);
        if (languageId.HasValue)
            query = query.Where(e => e.Card.Deck.LanguageId == languageId.Value && !e.Card.Deck.IsDeleted);
        if (!string.IsNullOrWhiteSpace(level))
        {
            var levelTag = "course-level:" + level.ToLower();
            query = query.Where(e => !e.Card.Deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:") && tag.Tag.ToLower() != levelTag));
        }
        return await query
            .OrderByDescending(e => e.ReviewedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<SrsRating, int>> GetRatingCountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewEvents.AsNoTracking()
            .Where(review => review.UserId == userId)
            .GroupBy(review => review.Rating)
            .Select(group => new { Rating = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Rating, item => item.Count, cancellationToken);
    }

    public Task<int> CountReviewsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        _context.ReviewEvents.AsNoTracking()
            .CountAsync(review => review.UserId == userId && review.ReviewedAt >= fromUtc && review.ReviewedAt < toUtc, cancellationToken);

    public Task<int> CountDueCardsAsync(Guid userId, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null)
    {
        var now = DateTime.UtcNow;
        var query = _context.Cards.AsNoTracking().Where(card => card.Deck.OwnerId == userId && !card.Deck.IsDeleted);
        if (languageId.HasValue) query = query.Where(card => card.Deck.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level))
        {
            var levelTag = "course-level:" + level.ToLower();
            query = query.Where(card => !card.Deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:") && tag.Tag.ToLower() != levelTag));
        }
        return query.CountAsync(card =>
            _context.CardSchedules.Any(schedule => schedule.UserId == userId && schedule.CardId == card.Id && schedule.DueAt <= now), cancellationToken);
    }

    public Task<List<DateTime>> GetReviewTimesSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        _context.ReviewEvents.AsNoTracking()
            .Where(review => review.UserId == userId && review.ReviewedAt >= sinceUtc)
            .Select(review => review.ReviewedAt)
            .ToListAsync(cancellationToken);

    public async Task<SrsEvaluationReadinessResponse> GetEvaluationReadinessAsync(CancellationToken cancellationToken = default)
    {
        var reviews = _context.ReviewEvents.AsNoTracking();
        var due = reviews.Where(item => item.WasDue == true && item.PreviousIntervalDays != null);
        return new SrsEvaluationReadinessResponse
        {
            TotalReviews = await reviews.CountAsync(cancellationToken),
            DistinctLearners = await reviews.Select(item => item.UserId).Distinct().CountAsync(cancellationToken),
            DistinctCards = await reviews.Select(item => item.CardId).Distinct().CountAsync(cancellationToken),
            FirstReviewAt = await reviews.MinAsync(item => (DateTime?)item.ReviewedAt, cancellationToken),
            LastReviewAt = await reviews.MaxAsync(item => (DateTime?)item.ReviewedAt, cancellationToken),
            DueReviewsWithInterval = await due.CountAsync(cancellationToken),
            DueReviewsRecalled = await due.CountAsync(item => item.Rating != SrsRating.Again, cancellationToken),
            MissingDueFlag = await reviews.CountAsync(item => item.WasDue == null, cancellationToken),
            MissingPreviousInterval = await reviews.CountAsync(item => item.PreviousIntervalDays == null, cancellationToken),
            ReviewsByAlgorithm = await reviews.GroupBy(item => item.AlgorithmVersion)
                .Select(group => new { Version = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Version, item => item.Count, cancellationToken)
        };
    }
}
