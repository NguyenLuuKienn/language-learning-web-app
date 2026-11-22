using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class SyncRepository : ISyncRepository
{
    private readonly AppDbContext _context;

    public SyncRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsOperationProcessedAsync(Guid userId, Guid clientOperationId, CancellationToken cancellationToken = default)
    {
        return await _context.SyncOperations
            .AnyAsync(s => s.UserId == userId && s.ClientOperationId == clientOperationId, cancellationToken);
    }

    public async Task RecordOperationAsync(SyncOperation operation, CancellationToken cancellationToken = default)
    {
        await _context.SyncOperations.AddAsync(operation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SyncOperation>> GetRecentOperationsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.SyncOperations
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
