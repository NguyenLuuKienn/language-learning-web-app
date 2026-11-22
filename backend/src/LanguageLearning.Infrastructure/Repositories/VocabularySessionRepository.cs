using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public sealed class VocabularySessionRepository : IVocabularySessionRepository
{
    private readonly AppDbContext _context;
    public VocabularySessionRepository(AppDbContext context) => _context = context;

    public Task<VocabularySession?> GetActiveAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken) =>
        _context.VocabularySessions.Where(session => session.UserId == userId && session.LessonId == lessonId && session.CompletedAt == null)
            .OrderByDescending(session => session.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public Task<VocabularySession?> GetByIdAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken) =>
        _context.VocabularySessions.FirstOrDefaultAsync(session => session.UserId == userId && session.Id == sessionId, cancellationToken);

    public Task<int> CountCompletedAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken) =>
        _context.VocabularySessions.CountAsync(session => session.UserId == userId && session.LessonId == lessonId && session.CompletedAt != null, cancellationToken);

    public Task<List<VocabularySession>> GetCompletedByLessonsAsync(Guid userId, IReadOnlyCollection<Guid> lessonIds, CancellationToken cancellationToken) =>
        lessonIds.Count == 0 ? Task.FromResult(new List<VocabularySession>()) :
        _context.VocabularySessions.AsNoTracking()
            .Where(session => session.UserId == userId && session.CompletedAt != null && lessonIds.Contains(session.LessonId))
            .OrderBy(session => session.CreatedAt).ToListAsync(cancellationToken);

    public Task<List<Course>> GetCandidateCoursesAsync(Guid? languageId, string? level, IReadOnlyList<string>? domains, CancellationToken cancellationToken)
    {
        var query = _context.Courses.AsNoTracking().Where(course => course.Status == ContentStatus.Published);
        if (languageId.HasValue) query = query.Where(course => course.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level)) query = query.Where(course => course.Level.ToLower() == level.ToLower());
        if (domains is { Count: > 0 })
        {
            var normalized = domains.Select(domain => domain.ToLower()).ToArray();
            query = query.Where(course => normalized.Contains(course.Domain.ToLower()));
        }
        return query.Include(course => course.Units).ThenInclude(unit => unit.Lessons)
            .ThenInclude(lesson => lesson.Vocabularies)
            .AsSplitQuery().ToListAsync(cancellationToken);
    }

    public Task<Dictionary<Guid, int>> GetCompletedCountsAsync(Guid userId, IReadOnlyCollection<Guid> lessonIds, CancellationToken cancellationToken)
    {
        if (lessonIds.Count == 0) return Task.FromResult(new Dictionary<Guid, int>());
        return _context.VocabularySessions.AsNoTracking()
            .Where(session => session.UserId == userId && session.CompletedAt != null && lessonIds.Contains(session.LessonId))
            .GroupBy(session => session.LessonId)
            .Select(group => new { LessonId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.LessonId, item => item.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, VocabularySession>> GetActiveByLessonAsync(Guid userId, IReadOnlyCollection<Guid> lessonIds, CancellationToken cancellationToken)
    {
        if (lessonIds.Count == 0) return [];
        var sessions = await _context.VocabularySessions.AsNoTracking()
            .Where(session => session.UserId == userId && session.CompletedAt == null && lessonIds.Contains(session.LessonId))
            .OrderByDescending(session => session.CreatedAt).ToListAsync(cancellationToken);
        return sessions.GroupBy(session => session.LessonId).ToDictionary(group => group.Key, group => group.First());
    }

    public async Task SaveAsync(VocabularySession session, CancellationToken cancellationToken)
    {
        if (_context.Entry(session).State == EntityState.Detached) _context.VocabularySessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(VocabularySession session, Guid languageId, string level, string lessonTitle,
        IReadOnlyList<VocabularyReviewSeed> reviewSeeds, bool completeLesson, CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            if (await _context.VocabularySessions.AsNoTracking().AnyAsync(item =>
                    item.Id == session.Id && item.UserId == session.UserId && item.CompletedAt != null, cancellationToken))
                return;
            _context.VocabularySessions.Update(session);
            await CompleteCoreAsync(session, languageId, level, lessonTitle, reviewSeeds, completeLesson, cancellationToken);
        });
    }

    private async Task CompleteCoreAsync(VocabularySession session, Guid languageId, string level, string lessonTitle,
        IReadOnlyList<VocabularyReviewSeed> reviewSeeds, bool completeLesson, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var lessonTag = "vocab-lesson:" + session.LessonId;
        var levelTag = "course-level:" + level.ToLowerInvariant();
        var deck = await _context.Decks
            .FirstOrDefaultAsync(item => item.OwnerId == session.UserId && !item.IsDeleted &&
                item.LanguageId == languageId && item.Tags.Any(tag => tag.Tag == lessonTag), cancellationToken);
        if (deck == null)
        {
            deck = new Deck
            {
                Id = Guid.NewGuid(), OwnerId = session.UserId, LanguageId = languageId,
                Title = lessonTitle, Description = string.Empty, IsPublic = false,
                Tags =
                [
                    new DeckTag { Id = Guid.NewGuid(), Tag = lessonTag },
                    new DeckTag { Id = Guid.NewGuid(), Tag = levelTag }
                ]
            };
        }

        var fronts = reviewSeeds.Select(seed => seed.Word.ToLowerInvariant()).ToArray();
        var existing = await _context.Cards.AsNoTracking()
            .Where(card => card.Deck.OwnerId == session.UserId && !card.Deck.IsDeleted &&
                card.Deck.LanguageId == languageId && fronts.Contains(card.Front.ToLower()) &&
                !card.Deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:") && tag.Tag.ToLower() != levelTag))
            .Select(card => new { card.Id, card.Front })
            .ToListAsync(cancellationToken);
        var existingMap = existing.GroupBy(card => card.Front.ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.First().Id);
        var existingIds = existingMap.Values.ToArray();
        var scheduledIds = existingIds.Length == 0 ? new HashSet<Guid>() :
            (await _context.CardSchedules.AsNoTracking()
                .Where(schedule => schedule.UserId == session.UserId && existingIds.Contains(schedule.CardId))
                .Select(schedule => schedule.CardId).ToListAsync(cancellationToken)).ToHashSet();
        var sourcedIds = (await _context.CardLessonSources.AsNoTracking()
            .Where(source => source.UserId == session.UserId && source.LessonId == session.LessonId)
            .Select(source => source.CardId).ToListAsync(cancellationToken)).ToHashSet();

        foreach (var seed in reviewSeeds)
        {
            if (!existingMap.TryGetValue(seed.Word.ToLowerInvariant(), out var cardId))
            {
                if (_context.Entry(deck).State == EntityState.Detached) _context.Decks.Add(deck);
                cardId = Guid.NewGuid();
                _context.Cards.Add(new Card
                {
                    Id = cardId, DeckId = deck.Id, Front = seed.Word, Back = seed.Meaning,
                    MeaningsJson = seed.MeaningsJson, Phonetic = seed.Phonetic,
                    PartOfSpeech = seed.PartOfSpeech, Example = seed.Example,
                    AudioUrl = string.IsNullOrWhiteSpace(seed.AudioUrl) ? null : seed.AudioUrl
                });
                existingMap[seed.Word.ToLowerInvariant()] = cardId;
            }
            if (!scheduledIds.Contains(cardId))
            {
                _context.CardSchedules.Add(new CardSchedule
                {
                    Id = Guid.NewGuid(), UserId = session.UserId, CardId = cardId,
                    DueAt = seed.DueAt, StateJson = seed.StateJson,
                    AlgorithmVersion = seed.AlgorithmVersion
                });
                scheduledIds.Add(cardId);
            }
            if (sourcedIds.Add(cardId))
                _context.CardLessonSources.Add(new CardLessonSource
                {
                    Id = Guid.NewGuid(), UserId = session.UserId,
                    LessonId = session.LessonId, CardId = cardId
                });
        }
        if (completeLesson)
        {
            var progress = await _context.LessonProgresses.FirstOrDefaultAsync(item =>
                item.UserId == session.UserId && item.LessonId == session.LessonId, cancellationToken);
            var firstCompletion = progress?.Status != "Completed";
            if (progress == null)
            {
                progress = new LessonProgress
                {
                    Id = Guid.NewGuid(), UserId = session.UserId, LessonId = session.LessonId
                };
                _context.LessonProgresses.Add(progress);
            }
            progress.Status = "Completed";
            progress.CompletedAt ??= session.CompletedAt;
            progress.LastAccessedAt = session.CompletedAt!.Value;
            progress.UpdatedAt = session.CompletedAt.Value;
            if (firstCompletion)
            {
                var timezone = await _context.Users.Where(item => item.Id == session.UserId)
                    .Select(item => item.Timezone).FirstAsync(cancellationToken);
                TimeZoneInfo zone;
                try { zone = TimeZoneInfo.FindSystemTimeZoneById(timezone); }
                catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
                catch (InvalidTimeZoneException) { zone = TimeZoneInfo.Utc; }
                var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(session.CompletedAt.Value, zone));
                var daily = await _context.DailyLearningStats.FirstOrDefaultAsync(item =>
                    item.UserId == session.UserId && item.LocalDate == localDate, cancellationToken);
                if (daily == null)
                    _context.DailyLearningStats.Add(new DailyLearningStats
                    {
                        Id = Guid.NewGuid(), UserId = session.UserId, LocalDate = localDate,
                        Timezone = timezone, LessonsCompleted = 1
                    });
                else daily.LessonsCompleted++;
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
