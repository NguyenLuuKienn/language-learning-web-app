using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public sealed class SpecialPracticeAttempt : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public Guid ContentId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string ContentSnapshotJson { get; set; } = "{}";
    public string AnswersJson { get; set; } = "{}";
    public string Status { get; set; } = "InProgress";
    public int ObjectiveScore { get; set; }
    public int ObjectiveMaxScore { get; set; }
    public int SubjectiveScore { get; set; }
    public int SubjectiveMaxScore { get; set; }
    public string EvaluationJson { get; set; } = "{}";
    public DateTime? ReviewedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
