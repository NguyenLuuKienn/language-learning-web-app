namespace LanguageLearning.Application.DTOs.AdminContent;

public record LessonVocabularyResponse(
    Guid Id,
    string Term,
    string Phonetic,
    string Meaning,
    string Example,
    string? AudioUrl,
    string? ImageUrl);

public record LessonViewerResponse(
    Guid Id,
    Guid UnitId,
    Guid CourseId,
    string CourseTitle,
    string UnitTitle,
    string Title,
    string Content,
    Guid? PreviousLessonId,
    Guid? NextLessonId,
    IReadOnlyList<LessonVocabularyResponse> Vocabulary);
