using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public sealed class LessonStepProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public string StepId { get; set; } = string.Empty;
    public string Status { get; set; } = "InProgress";
    public string? ActivityId { get; set; }
    public int Cursor { get; set; }
    public int Revision { get; set; }
    public string DraftJson { get; set; } = "{}";
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
