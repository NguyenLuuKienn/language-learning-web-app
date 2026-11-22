using LanguageLearning.Application.DTOs.Sharing;

namespace LanguageLearning.Application.Interfaces;

public interface ISharingService
{
    Task<List<PublicDeckResponse>> GetPublicDecksAsync(Guid? languageId, string? search, CancellationToken cancellationToken = default);
    Task<PublicDeckPreviewResponse> GetPublicDeckPreviewAsync(Guid deckId, CancellationToken cancellationToken = default);
    Task<CloneDeckResponse> CloneDeckAsync(Guid deckId, Guid userId, CancellationToken cancellationToken = default);
}
