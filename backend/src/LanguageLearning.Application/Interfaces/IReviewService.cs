
using LanguageLearning.Application.DTOs.Reviews;

namespace LanguageLearning.Application.Interfaces;

public interface IReviewService
{
    Task<List<DueCardResponse>> GetDueCardsAsync(
        Guid deckId,
        CancellationToken cancellationToken = default);
}
