namespace LanguageLearning.Application.DTOs.Courses;

public class UnitResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<LessonSummaryResponse> Lessons { get; set; } = new();
}
