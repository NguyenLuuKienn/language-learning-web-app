using LanguageLearning.Domain.Entities;
using LanguageLearning.Application.DTOs.Courses;

namespace LanguageLearning.Application.Interfaces;

public interface ICourseRepository
{
    Task<List<Language>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<Language?> GetLanguageByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Language?> GetLanguageByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddLanguageAsync(Language language, CancellationToken cancellationToken = default);
    Task UpdateLanguageAsync(Language language, CancellationToken cancellationToken = default);
    Task DeleteLanguageAsync(Language language, CancellationToken cancellationToken = default);

    Task<List<CourseResponse>> GetCourseSummariesAsync(Guid? languageId, string? level, string? domain, Guid? userId, CancellationToken cancellationToken = default);
    Task<List<Course>> GetAllCoursesForExportAsync(Guid? languageId = null, CancellationToken cancellationToken = default);
    Task<Course?> GetCourseByIdAsync(Guid id, bool includeUnitsAndLessons = false, CancellationToken cancellationToken = default);
    Task AddCourseAsync(Course course, CancellationToken cancellationToken = default);
    Task UpdateCourseAsync(Course course, CancellationToken cancellationToken = default);
    Task SaveCourseMaterialAsync(Course course, List<Unit> units, List<Deck> decks, CancellationToken cancellationToken = default);
    Task SaveEditedLessonMaterialAsync(Course course, Guid lessonId, IReadOnlyList<Vocabulary> vocabularies, IReadOnlyList<string> vocabularyMaterialIds, IReadOnlyList<Question> questions, IReadOnlyDictionary<string, (string Term, string Meaning)> oldVocabulary, IReadOnlyDictionary<string, string> oldQuestionPrompts, CancellationToken cancellationToken = default);
    Task RemapLessonStepsAsync(Guid courseId, Guid lessonId, string expectedMaterialJson, string updatedMaterialJson,
        IReadOnlyDictionary<string, string> stepIds, string newContentVersion, CancellationToken cancellationToken = default);
    Task DeleteCourseAsync(Course course, CancellationToken cancellationToken = default);

    Task<Unit?> GetUnitByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddUnitAsync(Unit unit, CancellationToken cancellationToken = default);
    Task UpdateUnitAsync(Unit unit, CancellationToken cancellationToken = default);
    Task DeleteUnitAsync(Unit unit, CancellationToken cancellationToken = default);

    Task<Lesson?> GetLessonByIdAsync(Guid id, bool includeVocabularies = false, CancellationToken cancellationToken = default);
    Task AddLessonAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task UpdateLessonAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task DeleteLessonAsync(Lesson lesson, CancellationToken cancellationToken = default);

    Task<bool> IsEnrolledAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);
    Task EnrollAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);
    Task<List<LessonProgress>> GetUserLessonProgressesAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);
}
