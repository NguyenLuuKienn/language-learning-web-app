using LanguageLearning.Application.DTOs.Statistics;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDeckRepository _deckRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;

    public StatisticsService(
        IReviewRepository reviewRepository,
        IUserRepository userRepository,
        IDeckRepository deckRepository,
        IProgressRepository progressRepository,
        IStudyAccessService access,
        ICurrentUserService currentUser)
    {
        _reviewRepository = reviewRepository;
        _userRepository = userRepository;
        _deckRepository = deckRepository;
        _progressRepository = progressRepository;
        _access = access;
        _currentUser = currentUser;
    }

    public Task<List<LeaderboardEntryResponse>> GetWeeklyLeaderboardAsync(CancellationToken cancellationToken = default) =>
        _progressRepository.GetWeeklyLeaderboardAsync(cancellationToken);

    public async Task<StatisticsOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var timezone = user?.Timezone ?? "Asia/Ho_Chi_Minh";

        var ratingCounts = await _reviewRepository.GetRatingCountsAsync(userId, cancellationToken);
        int totalReviews = ratingCounts.Values.Sum();

        var ratingDist = new Dictionary<string, int>
        {
            [SrsRating.Again.ToString()] = ratingCounts.GetValueOrDefault(SrsRating.Again),
            [SrsRating.Hard.ToString()] = ratingCounts.GetValueOrDefault(SrsRating.Hard),
            [SrsRating.Good.ToString()] = ratingCounts.GetValueOrDefault(SrsRating.Good),
            [SrsRating.Easy.ToString()] = ratingCounts.GetValueOrDefault(SrsRating.Easy)
        };

        int positiveReviews = ratingDist[SrsRating.Good.ToString()] + ratingDist[SrsRating.Easy.ToString()];
        double retentionRate = totalReviews > 0 ? Math.Round((double)positiveReviews / totalReviews * 100, 1) : 0;

        var target = _currentUser.IsAdmin() ? null : await _access.GetTargetAsync(userId, cancellationToken);
        var dueCardsCount = !_currentUser.IsAdmin() && target == null ? 0 :
            await _reviewRepository.CountDueCardsAsync(userId, cancellationToken, target?.LanguageId, target?.Level);
        var totalDecks = !_currentUser.IsAdmin() && target == null ? 0 :
            (await _deckRepository.GetAccessibleDecksAsync(userId, 1, 1, cancellationToken, target?.LanguageId, target?.Level)).TotalItems;

        var domains = target?.Domains?.Count > 0 ? target.Domains : target == null ? null : [target.Domain];
        int totalLessonsCompleted = !_currentUser.IsAdmin() && target == null ? 0 :
            await _progressRepository.CountCompletedLessonsAsync(userId, cancellationToken, target?.LanguageId, target?.Level, domains);

        var userGoal = await _userRepository.GetUserGoalAsync(userId, cancellationToken);
        int currentStreak = await _userRepository.GetCurrentStreakAsync(userId, timezone, cancellationToken);

        TimeZoneInfo tzInfo;
        try { tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone); } catch { tzInfo = TimeZoneInfo.Utc; }
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
        var today = DateOnly.FromDateTime(localNow);

        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(today.ToDateTime(TimeOnly.MinValue), tzInfo);
        var tomorrowStartUtc = TimeZoneInfo.ConvertTimeToUtc(today.AddDays(1).ToDateTime(TimeOnly.MinValue), tzInfo);
        int todayReviewsCount = await _reviewRepository.CountReviewsAsync(userId, todayStartUtc, tomorrowStartUtc, cancellationToken);

        var schedules = await _reviewRepository.GetSchedulesByUserAsync(userId, cancellationToken);
        int cardsMastered = schedules.Count(s =>
        {
            var state = SrsState.FromJson(s.StateJson);
            return state.Repetitions >= 3 && state.IntervalDays >= 6;
        });
        int cardsLearning = schedules.Count - cardsMastered;
        if (cardsLearning < 0) cardsLearning = 0;

        return new StatisticsOverviewResponse
        {
            TotalReviews = totalReviews,
            TotalLessonsCompleted = totalLessonsCompleted,
            RetentionRate = retentionRate,
            DueCardsCount = dueCardsCount,
            TotalDecksCount = totalDecks,
            CurrentStreak = currentStreak,
            DailyTarget = userGoal?.DailyTarget ?? 10,
            CardsMastered = cardsMastered,
            CardsLearning = cardsLearning,
            TodayReviewsCount = todayReviewsCount,
            RatingDistribution = ratingDist
        };
    }

    public async Task<List<HeatmapDayResponse>> GetHeatmapAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var timezone = user?.Timezone ?? "Asia/Ho_Chi_Minh";

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
        var startDate = today.AddDays(-364);

        var sinceUtc = TimeZoneInfo.ConvertTimeToUtc(startDate.ToDateTime(TimeOnly.MinValue), tzInfo);
        var reviewTimes = await _reviewRepository.GetReviewTimesSinceAsync(userId, sinceUtc, cancellationToken);
        var dailyCountMap = new Dictionary<DateOnly, int>();

        foreach (var reviewedAt in reviewTimes)
        {
            var localReviewDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(reviewedAt, tzInfo));
            if (localReviewDate >= startDate && localReviewDate <= today)
            {
                dailyCountMap[localReviewDate] = dailyCountMap.GetValueOrDefault(localReviewDate) + 1;
            }
        }

        var heatmap = new List<HeatmapDayResponse>();
        for (var d = startDate; d <= today; d = d.AddDays(1))
        {
            int count = dailyCountMap.GetValueOrDefault(d, 0);
            int level = count switch
            {
                0 => 0,
                <= 3 => 1,
                <= 9 => 2,
                <= 19 => 3,
                _ => 4
            };

            heatmap.Add(new HeatmapDayResponse
            {
                Date = d.ToString("yyyy-MM-dd"),
                Count = count,
                Level = level
            });
        }

        return heatmap;
    }
}
