namespace LanguageLearning.Application.DTOs.Cards;

public sealed class SrsEvaluationReadinessResponse
{
    public int TotalReviews { get; set; }
    public int DistinctLearners { get; set; }
    public int DistinctCards { get; set; }
    public DateTime? FirstReviewAt { get; set; }
    public DateTime? LastReviewAt { get; set; }
    public int DueReviewsWithInterval { get; set; }
    public int DueReviewsRecalled { get; set; }
    public int MissingDueFlag { get; set; }
    public int MissingPreviousInterval { get; set; }
    public Dictionary<string, int> ReviewsByAlgorithm { get; set; } = new();
}
