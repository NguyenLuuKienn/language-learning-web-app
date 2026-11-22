using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IWritingSubmissionRepository
{
    Task<WritingSubmission?> GetLatestAsync(Guid userId, string promptId, CancellationToken cancellationToken = default);
    Task<WritingSubmission> AddAsync(WritingSubmission submission, CancellationToken cancellationToken = default);
}
