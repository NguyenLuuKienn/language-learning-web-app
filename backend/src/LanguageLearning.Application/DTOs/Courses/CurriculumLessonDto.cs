namespace LanguageLearning.Application.DTOs.Courses;

public class CurriculumLessonDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string LessonType { get; set; } = "Vocabulary";
    public int SortOrder { get; set; }
    public List<CurriculumVocabDto> Vocabularies { get; set; } = new();
}
