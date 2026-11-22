using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IPlacementReviewRepository
{
    Task AddAttemptAsync(PlacementAttempt attempt, CancellationToken cancellationToken = default);
    Task<PlacementAttempt?> GetAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default);
    Task<PlacementAttempt?> GetLatestAttemptAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);
    Task<List<PlacementAttempt>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<(Dictionary<Guid, string> Learners, Dictionary<Guid, string> Courses)> GetPendingLabelsAsync(
        IReadOnlyCollection<Guid> userIds, IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default);
    Task SaveAttemptAsync(CancellationToken cancellationToken = default);
    Task AddAudioAsync(PlacementAudio audio, CancellationToken cancellationToken = default);
    Task<bool> OwnsAudioAsync(Guid userId, Guid courseId, string questionId, string fileName, CancellationToken cancellationToken = default);
    Task<bool> OwnsFileAsync(Guid userId, string fileName, CancellationToken cancellationToken = default);
}
