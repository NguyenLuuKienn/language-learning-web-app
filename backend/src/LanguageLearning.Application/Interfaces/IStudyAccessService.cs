using LanguageLearning.Application.DTOs.Auth;

namespace LanguageLearning.Application.Interfaces;

public interface IStudyAccessService
{
    Task<StudyTargetDto?> GetTargetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessDeckAsync(Guid userId, Guid deckId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessCardAsync(Guid userId, Guid cardId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessLessonAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessVocabularyAsync(Guid userId, Guid vocabularyId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessMediaAsync(Guid userId, string fileName, CancellationToken cancellationToken = default);
}
