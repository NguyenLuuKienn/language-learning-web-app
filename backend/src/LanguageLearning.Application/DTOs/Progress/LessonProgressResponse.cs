namespace LanguageLearning.Application.DTOs.Progress;

public class LessonProgressResponse
{
    public Guid LessonId { get; set; }
    public string Status { get; set; } = "InProgress";
    public DateTime? CompletedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
}
