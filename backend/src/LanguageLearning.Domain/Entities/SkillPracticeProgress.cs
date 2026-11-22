using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public sealed class SkillPracticeProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public string Status { get; set; } = "InProgress";
    public int Attempts { get; set; }
    public int CorrectAttempts { get; set; }
}
