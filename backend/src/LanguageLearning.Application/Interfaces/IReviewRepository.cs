using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Application.DTOs.Cards;

namespace LanguageLearning.Application.Interfaces;

public interface IReviewRepository
{
    Task<CardSchedule?> GetScheduleAsync(Guid userId, Guid cardId, CancellationToken cancellationToken = default);
    Task<List<CardSchedule>> GetSchedulesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CardSchedule>> GetSchedulesForCardsAsync(Guid userId, IReadOnlyCollection<Guid> cardIds, CancellationToken cancellationToken = default);
    Task UpsertScheduleAsync(CardSchedule schedule, CancellationToken cancellationToken = default);

    Task<List<Card>> GetDueCardsAsync(Guid userId, Guid? deckId, int limit = 50, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null, bool dueOnly = false, bool newOnly = false, Guid? lessonId = null);
    Task<ReviewOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null, Guid? deckId = null, Guid? lessonId = null);
    Task<List<ReviewLessonSourceResponse>> GetLessonSourcesAsync(Guid userId, Guid? languageId, string? level, CancellationToken cancellationToken = default);
    Task<ReviewEvent?> GetExistingReviewEventAsync(Guid userId, Guid clientEventId, CancellationToken cancellationToken = default);
    Task AddReviewEventAsync(ReviewEvent reviewEvent, CancellationToken cancellationToken = default);
    Task<bool> CommitReviewAsync(CardSchedule schedule, ReviewEvent reviewEvent, string timezone, CancellationToken cancellationToken = default);
    Task RecordDailyReviewActivityAsync(Guid userId, string timezone, CancellationToken cancellationToken = default);

    Task<List<ReviewEvent>> GetReviewHistoryAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null);
    Task<Dictionary<SrsRating, int>> GetRatingCountsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountReviewsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<int> CountDueCardsAsync(Guid userId, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null);
    Task<List<DateTime>> GetReviewTimesSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default);
    Task<SrsEvaluationReadinessResponse> GetEvaluationReadinessAsync(CancellationToken cancellationToken = default);
}
