namespace LanguageLearning.Application.DTOs.Cards;

public sealed class ReviewOverviewResponse
{
    public int DueCount { get; init; }
    public int LearningCount { get; init; }
    public int UnseenCount { get; init; }
    public int TotalCount { get; init; }
    public DateTime? NextDueAt { get; init; }
    public int ReviewCount { get; init; }
    public int RelearningCount { get; init; }
    public int[] DueForecast7Days { get; init; } = new int[7];
    public int ObservedDueReviews { get; init; }
    public int? ObservedRecallPercent { get; init; }
}
