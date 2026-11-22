using LanguageLearning.Application.DTOs.Progress;

namespace LanguageLearning.Application.Interfaces;

public interface ILessonJourneyService
{
    Task<LessonJourneyResponse> GetAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken);
    Task<LessonJourneyResponse> VisitAsync(Guid userId, Guid lessonId, string stepId, SaveLessonJourneyStepRequest request, CancellationToken cancellationToken);
    Task<LessonJourneyResponse> RefreshAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken);
}
