using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public sealed class LearningSession : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public string ContentVersion { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public sealed class LearningEvidence : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public string StepId { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string ContentVersion { get; set; } = string.Empty;
    public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
}
