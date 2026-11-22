namespace LanguageLearning.Application.DTOs.Statistics;

public class StatisticsOverviewResponse
{
    public int TotalReviews { get; set; }
    public int TotalCardsReviewed => TotalReviews;
    public int TotalLessonsCompleted { get; set; }
    public double RetentionRate { get; set; }
    public double RetentionRatePercentage => RetentionRate;
    public int DueCardsCount { get; set; }
    public int TotalDecksCount { get; set; }
    public int CurrentStreak { get; set; }
    public int CurrentStreakDays => CurrentStreak;
    public int DailyTarget { get; set; }
    public int DailyGoal => DailyTarget;
    public int CardsMastered { get; set; }
    public int CardsLearning { get; set; }
    public int TodayReviewsCount { get; set; }
    public bool GoalMetToday => TodayReviewsCount >= DailyTarget;
    public Dictionary<string, int> RatingDistribution { get; set; } = new();
}

