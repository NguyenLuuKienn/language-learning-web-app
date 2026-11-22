using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IAdminRepository
{
    Task<List<User>> GetUsersAsync(string? search, CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetAuditLogsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task RecordAuditLogAsync(AuditLog log, CancellationToken cancellationToken = default);
}
