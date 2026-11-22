using LanguageLearning.Domain.Entities;
using LanguageLearning.Application.DTOs.Statistics;
using LanguageLearning.Application.DTOs.Progress;

namespace LanguageLearning.Application.Interfaces;

public interface IProgressRepository
{
    Task<LessonProgress?> GetProgressAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default);
    Task UpsertProgressAsync(LessonProgress progress, CancellationToken cancellationToken = default);
    Task<int> CountCompletedLessonsAsync(Guid userId, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null, IReadOnlyCollection<string>? domains = null);
    Task<LessonProgress?> GetLastAccessedProgressAsync(Guid userId, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null, IReadOnlyCollection<string>? domains = null);
    Task<LearningRoadmapResponse> GetRoadmapAsync(Guid userId, Guid languageId, string level, IReadOnlyCollection<string> domains, CancellationToken cancellationToken = default);
    Task RecordLessonCompletionActivityAsync(Guid userId, string timezone, CancellationToken cancellationToken = default);
    Task<List<LeaderboardEntryResponse>> GetWeeklyLeaderboardAsync(CancellationToken cancellationToken = default);
}
