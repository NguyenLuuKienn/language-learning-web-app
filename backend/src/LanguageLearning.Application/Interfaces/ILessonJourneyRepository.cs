using LanguageLearning.Domain.Entities;
using LanguageLearning.Application.DTOs.Progress;

namespace LanguageLearning.Application.Interfaces;

public interface ILessonJourneyRepository
{
    Task<List<LessonStepProgress>> GetStepsAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken);
    Task SaveStepAsync(LessonStepProgress step, CancellationToken cancellationToken);
    Task<LearningSession?> GetSessionAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken);
    Task<bool> HasLearnerStateAsync(Guid lessonId, CancellationToken cancellationToken);
    Task<List<LearningWeakPointResponse>> GetWeakPointsAsync(Guid userId, Guid courseId, Guid lessonId,
        IReadOnlyCollection<(string Kind, string ActivityId)> skillActivities, CancellationToken cancellationToken);
    Task RecordJourneyAsync(Guid userId, Guid lessonId, string contentVersion, bool completed,
        IReadOnlyCollection<(string StepId, string StepType)> completedSteps, CancellationToken cancellationToken);
}
