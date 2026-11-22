using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.DTOs.Courses;

public class LessonSummaryResponse
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string LessonType { get; set; } = "Vocabulary";
    public int SortOrder { get; set; }
    public ContentStatus Status { get; set; }
    public int VocabCount { get; set; }
    public string ProgressStatus { get; set; } = "NotStarted";
}
