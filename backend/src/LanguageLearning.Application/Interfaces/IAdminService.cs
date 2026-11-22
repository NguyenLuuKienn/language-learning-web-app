using LanguageLearning.Application.DTOs.Admin;
using LanguageLearning.Application.DTOs.Sharing;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Interfaces;

public interface IAdminService
{
    Task<List<AdminUserResponse>> GetUsersAsync(string? search, CancellationToken cancellationToken = default);
    Task<AdminUserResponse> UpdateUserStatusAsync(Guid targetUserId, bool isActive, Guid actorId, CancellationToken cancellationToken = default);
    Task<AdminUserResponse> UpdateUserRoleAsync(Guid targetUserId, UserRole role, Guid actorId, CancellationToken cancellationToken = default);

    Task<List<PublicDeckResponse>> GetPublicDecksForModerationAsync(CancellationToken cancellationToken = default);
    Task UpdateDeckVisibilityAsync(Guid deckId, bool isPublic, Guid actorId, CancellationToken cancellationToken = default);

    Task<List<AuditLogResponse>> GetAuditLogsAsync(CancellationToken cancellationToken = default);
}
