namespace LanguageLearning.Application.DTOs.Courses;

public class VocabularyResponse
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public Guid LanguageId { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Phonetic { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public List<string> Meanings { get; set; } = new();
    public string PartOfSpeech { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
}
