namespace LanguageLearning.Application.DTOs.Courses;

public class LessonDetailResponse : LessonSummaryResponse
{
    public string Content { get; set; } = string.Empty;
    public List<VocabularyResponse> Vocabularies { get; set; } = new();
}
