namespace LanguageLearning.Application.DTOs.Courses;

public class CurriculumExportResponse
{
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public List<LanguageResponse> Languages { get; set; } = new();
    public List<CurriculumCourseDto> Courses { get; set; } = new();
}
