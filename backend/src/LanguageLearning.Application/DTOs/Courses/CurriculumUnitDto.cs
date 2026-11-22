namespace LanguageLearning.Application.DTOs.Courses;

public class CurriculumUnitDto
{
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<CurriculumLessonDto> Lessons { get; set; } = new();
}
