using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface ISyncRepository
{
    Task<bool> IsOperationProcessedAsync(Guid userId, Guid clientOperationId, CancellationToken cancellationToken = default);
    Task RecordOperationAsync(SyncOperation operation, CancellationToken cancellationToken = default);
    Task<List<SyncOperation>> GetRecentOperationsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);
}
