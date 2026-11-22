namespace LanguageLearning.Application.DTOs.Progress;

public class CourseProgressResponse
{
    public Guid CourseId { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public double Percentage => TotalLessons > 0 ? Math.Round((double)CompletedLessons / TotalLessons * 100, 1) : 0;
}
