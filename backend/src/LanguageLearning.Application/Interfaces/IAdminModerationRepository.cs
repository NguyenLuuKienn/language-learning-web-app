using LanguageLearning.Application.Common;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IAdminModerationRepository
{
    Task<PagedResponse<User>> ListUsersAsync(PaginationParams pagination, string? search, CancellationToken ct);
    Task<PagedResponse<Deck>> ListDecksAsync(PaginationParams pagination, string? search, CancellationToken ct);
    Task<User?> FindUserAsync(Guid id, CancellationToken ct);
    Task<Deck?> FindDeckAsync(Guid id, CancellationToken ct);
    Task SaveWithAuditAsync(AuditLog audit, CancellationToken ct);
}
