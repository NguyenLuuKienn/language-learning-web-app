namespace LanguageLearning.Application.DTOs.Courses;

public class CurriculumVocabDto
{
    public string Term { get; set; } = string.Empty;
    public string Phonetic { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
}
