using LanguageLearning.Application.DTOs.Cards;

namespace LanguageLearning.Application.Interfaces;

public interface ISrsService
{
    Task<ReviewOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default, Guid? deckId = null, Guid? lessonId = null);
    Task<DueCardsSummaryResponse> GetDueCardsAsync(Guid userId, Guid? deckId, int limit = 50, CancellationToken cancellationToken = default, bool dueOnly = false, bool newOnly = false, Guid? lessonId = null);
    Task<List<ReviewLessonSourceResponse>> GetLessonSourcesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ReviewResultResponse> SubmitReviewAsync(Guid userId, SubmitReviewRequest request, CancellationToken cancellationToken = default);
    Task<List<ReviewResultResponse>> GetHistoryAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);
}
