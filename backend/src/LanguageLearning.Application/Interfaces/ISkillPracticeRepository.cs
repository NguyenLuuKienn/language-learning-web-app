using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface ISkillPracticeRepository
{
    Task<List<SkillPracticeProgress>> GetForKindAsync(Guid userId, string kind, CancellationToken cancellationToken);
    Task<SkillPracticeProgress?> GetAsync(Guid userId, Guid courseId, string kind, string activityId, CancellationToken cancellationToken);
    Task SaveAsync(SkillPracticeProgress progress, CancellationToken cancellationToken);
    Task<(SkillPracticeProgress Progress, bool? Correct, bool Duplicate)> RecordAttemptAsync(
        SkillPracticeEvent attempt, CancellationToken cancellationToken);
}
