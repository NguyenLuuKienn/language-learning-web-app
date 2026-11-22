using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface ISpecialPracticeRepository
{
    Task<SpecialPracticeAttempt?> GetAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken);
    Task<SpecialPracticeAttempt?> GetActiveAsync(Guid userId, Guid courseId, Guid contentId, CancellationToken cancellationToken);
    Task<List<SpecialPracticeAttempt>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<SpecialPracticeAttempt>> GetPendingAsync(CancellationToken cancellationToken);
    Task<SpecialPracticeAttempt?> GetForReviewAsync(Guid attemptId, CancellationToken cancellationToken);
    Task SaveAsync(SpecialPracticeAttempt attempt, CancellationToken cancellationToken);
}
