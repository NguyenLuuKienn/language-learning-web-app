using LanguageLearning.Application.DTOs.Progress;

namespace LanguageLearning.Application.Interfaces;

public interface ISkillPracticeService
{
    Task<List<SkillPracticeResponse>> GetAsync(Guid userId, string kind, CancellationToken cancellationToken);
    Task<SkillPracticeResponse> RecordAsync(Guid userId, string kind, SkillPracticeRequest request, CancellationToken cancellationToken);
}
