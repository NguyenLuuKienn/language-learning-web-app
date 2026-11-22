namespace LanguageLearning.Application.DTOs.Progress;

public class ResumeLearningResponse
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public DateTime LastAccessedAt { get; set; }
    public string? StepId { get; set; }
    public string? ActivityId { get; set; }
    public int Cursor { get; set; }
}
