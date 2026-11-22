using LanguageLearning.Application.DTOs.Auth;

namespace LanguageLearning.Application.Interfaces;

public interface IPlacementService
{
    Task<PlacementExamDto> GetExamAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<PlacementResultDto> SubmitAsync(Guid userId, Guid courseId, PlacementSubmissionDto submission, CancellationToken cancellationToken = default);
    Task RegisterAudioAsync(Guid userId, Guid courseId, string questionId, string fileName, CancellationToken cancellationToken = default);
    Task<PlacementResultDto> GetResultAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken = default);
    Task<PlacementResultDto?> GetLatestResultAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);
    Task<List<PlacementReviewDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<PlacementResultDto> ReviewAsync(Guid adminId, Guid attemptId, PlacementReviewRequestDto review, CancellationToken cancellationToken = default);
}
