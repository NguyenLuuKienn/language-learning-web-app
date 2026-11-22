using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public sealed class VocabularySession : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public int GroupIndex { get; set; }
    public string StepsJson { get; set; } = "[]";
    public string ResultsJson { get; set; } = "[]";
    public int Position { get; set; }
    public DateTime? CompletedAt { get; set; }
}
