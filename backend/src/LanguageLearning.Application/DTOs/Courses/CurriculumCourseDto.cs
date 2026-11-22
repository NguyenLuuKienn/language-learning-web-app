namespace LanguageLearning.Application.DTOs.Courses;

public class CurriculumCourseDto
{
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = "A1";
    public string Domain { get; set; } = "General";
    public string Description { get; set; } = string.Empty;
    public List<CurriculumUnitDto> Units { get; set; } = new();
}
