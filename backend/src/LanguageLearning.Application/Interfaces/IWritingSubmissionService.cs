using LanguageLearning.Application.DTOs.Progress;

namespace LanguageLearning.Application.Interfaces;

public interface IWritingSubmissionService
{
    Task<WritingSubmissionDto?> GetLatestAsync(Guid userId, string promptId, CancellationToken cancellationToken = default);
    Task<WritingSubmissionDto> SaveAsync(Guid userId, SaveWritingSubmissionRequest request, CancellationToken cancellationToken = default);
}
