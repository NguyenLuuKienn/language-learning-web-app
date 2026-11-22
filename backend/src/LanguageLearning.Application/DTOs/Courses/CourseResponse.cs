using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.DTOs.Courses;

public class CourseResponse
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = "A1";
    public string Domain { get; set; } = "General";
    public string Description { get; set; } = string.Empty;
    public ContentStatus Status { get; set; }
    public int UnitsCount { get; set; }
    public int LessonsCount { get; set; }
    public bool IsEnrolled { get; set; }
    public bool HasMaterial { get; set; }
}
