using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public sealed class SkillPracticeEvent : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ClientAttemptId { get; set; }
    public Guid CourseId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public int? AnswerIndex { get; set; }
    public bool Practiced { get; set; }
    public bool? Correct { get; set; }
}
