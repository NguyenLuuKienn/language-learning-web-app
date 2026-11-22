using System.Text.Json;
using LanguageLearning.Application.DTOs.Progress;

namespace LanguageLearning.Application.Interfaces;

public interface ISpecialPracticeService
{
    Task<List<SpecialPracticeCatalogItem>> GetCatalogAsync(Guid userId, CancellationToken cancellationToken);
    Task<SpecialPracticeAttemptResponse> StartAsync(Guid userId, StartSpecialPracticeRequest request, CancellationToken cancellationToken);
    Task<SpecialPracticeAttemptResponse> GetAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken);
    Task<SpecialPracticeAttemptResponse> SaveAnswerAsync(Guid userId, Guid attemptId, SaveSpecialPracticeAnswerRequest request, CancellationToken cancellationToken);
    Task<SpecialPracticeAttemptResponse> SaveAudioAsync(Guid userId, Guid attemptId, Guid questionId, string audioUrl, CancellationToken cancellationToken);
    Task<SpecialPracticeAttemptResponse> SubmitAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken);
    Task<List<SpecialPracticeReviewItem>> GetPendingReviewsAsync(CancellationToken cancellationToken);
    Task<SpecialPracticeReviewDetail> GetReviewAsync(Guid attemptId, CancellationToken cancellationToken);
    Task<SpecialPracticeAttemptResponse> GradeAsync(Guid attemptId, GradeSpecialPracticeRequest request, CancellationToken cancellationToken);
    Task UpdateContentAsync(Guid courseId, JsonElement content, CancellationToken cancellationToken);
    Task<JsonElement> GetAdminContentAsync(Guid courseId, string kind, CancellationToken cancellationToken);
    Task<JsonElement> CreateAdminContentAsync(Guid courseId, string kind, JsonElement item, CancellationToken cancellationToken);
    Task<JsonElement> UpdateAdminContentAsync(Guid courseId, string kind, Guid itemId, JsonElement item, CancellationToken cancellationToken);
    Task DeleteAdminContentAsync(Guid courseId, string kind, Guid itemId, CancellationToken cancellationToken);
    Task<JsonElement> ImportAdminContentAsync(Guid courseId, string kind, JsonElement items, CancellationToken cancellationToken);
    void ValidateContent(JsonElement content, string level, string languageCode);
}
