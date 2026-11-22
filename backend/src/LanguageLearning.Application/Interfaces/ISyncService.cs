using LanguageLearning.Application.DTOs.Sync;

namespace LanguageLearning.Application.Interfaces;

public interface ISyncService
{
    Task<SyncPushResponse> PushOperationsAsync(Guid userId, SyncPushRequest request, CancellationToken cancellationToken = default);
    Task<SyncPullResponse> PullChangesAsync(Guid userId, DateTime? cursor, CancellationToken cancellationToken = default);
}
