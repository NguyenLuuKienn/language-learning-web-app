using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IVocabularySessionRepository
{
    Task<VocabularySession?> GetActiveAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken);
    Task<VocabularySession?> GetByIdAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
    Task<int> CountCompletedAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken);
    Task<List<VocabularySession>> GetCompletedByLessonsAsync(Guid userId, IReadOnlyCollection<Guid> lessonIds, CancellationToken cancellationToken);
    Task<List<Course>> GetCandidateCoursesAsync(Guid? languageId, string? level, IReadOnlyList<string>? domains, CancellationToken cancellationToken);
    Task<Dictionary<Guid, int>> GetCompletedCountsAsync(Guid userId, IReadOnlyCollection<Guid> lessonIds, CancellationToken cancellationToken);
    Task<Dictionary<Guid, VocabularySession>> GetActiveByLessonAsync(Guid userId, IReadOnlyCollection<Guid> lessonIds, CancellationToken cancellationToken);
    Task SaveAsync(VocabularySession session, CancellationToken cancellationToken);
    Task CompleteAsync(VocabularySession session, Guid languageId, string level, string lessonTitle,
        IReadOnlyList<VocabularyReviewSeed> reviewSeeds, bool completeLesson, CancellationToken cancellationToken);
}

public sealed record VocabularyReviewSeed(
    string Word, string Meaning, string MeaningsJson, string Phonetic, string PartOfSpeech,
    string Example, string AudioUrl, string StateJson, DateTime DueAt, string AlgorithmVersion);
