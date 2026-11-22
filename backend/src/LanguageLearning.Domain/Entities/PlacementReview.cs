using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public sealed class PlacementAttempt : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string QuestionsJson { get; set; } = "[]";
    public string AnswersJson { get; set; } = "{}";
    public string AudioFilesJson { get; set; } = "{}";
    public string ScoresJson { get; set; } = "{}";
    public string Status { get; set; } = "Pending";
    public string Feedback { get; set; } = string.Empty;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public sealed class PlacementAudio : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string QuestionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
