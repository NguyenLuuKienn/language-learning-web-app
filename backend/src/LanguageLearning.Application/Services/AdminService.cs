using System.Text.Json;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Admin;
using LanguageLearning.Application.DTOs.Sharing;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDeckRepository _deckRepository;

    public AdminService(
        IAdminRepository adminRepository,
        IUserRepository userRepository,
        IDeckRepository deckRepository)
    {
        _adminRepository = adminRepository;
        _userRepository = userRepository;
        _deckRepository = deckRepository;
    }

    public async Task<List<AdminUserResponse>> GetUsersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var users = await _adminRepository.GetUsersAsync(search, cancellationToken);

        return users.Select(u => new AdminUserResponse
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            Role = u.Role,
            Timezone = u.Timezone,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList();
    }

    public async Task<AdminUserResponse> UpdateUserStatusAsync(Guid targetUserId, bool isActive, Guid actorId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(Messages.Auth.UserNotFound);
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _adminRepository.RecordAuditLogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = isActive ? "UserActivated" : "UserDeactivated",
            TargetType = "User",
            TargetId = targetUserId,
            MetadataJson = JsonSerializer.Serialize(new { email = user.Email, newStatus = isActive }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);

        return new AdminUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            Timezone = user.Timezone,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<AdminUserResponse> UpdateUserRoleAsync(Guid targetUserId, UserRole role, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentException(Messages.Auth.InvalidRole);
        var user = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(Messages.Auth.UserNotFound);
        }

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _adminRepository.RecordAuditLogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = "UserRoleUpdated",
            TargetType = "User",
            TargetId = targetUserId,
            MetadataJson = JsonSerializer.Serialize(new { email = user.Email, newRole = role.ToString() }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);

        return new AdminUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            Timezone = user.Timezone,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<List<PublicDeckResponse>> GetPublicDecksForModerationAsync(CancellationToken cancellationToken = default)
    {
        return await _deckRepository.GetDeckSummariesAsync(null, null, true, cancellationToken);
    }

    public async Task UpdateDeckVisibilityAsync(Guid deckId, bool isPublic, Guid actorId, CancellationToken cancellationToken = default)
    {
        var deck = await _deckRepository.GetByIdAsync(deckId, cancellationToken);
        if (deck == null)
        {
            throw new NotFoundException(Messages.Deck.DeckNotFound);
        }

        deck.IsPublic = isPublic;
        deck.UpdatedAt = DateTime.UtcNow;
        _deckRepository.Update(deck);
        await _deckRepository.SaveChangesAsync(cancellationToken);

        await _adminRepository.RecordAuditLogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = isPublic ? "DeckPublishedByAdmin" : "DeckHiddenByAdmin",
            TargetType = "Deck",
            TargetId = deckId,
            MetadataJson = JsonSerializer.Serialize(new { title = deck.Title, isPublic }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<List<AuditLogResponse>> GetAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        var logs = await _adminRepository.GetAuditLogsAsync(100, cancellationToken);

        return logs.Select(l => new AuditLogResponse
        {
            Id = l.Id,
            ActorId = l.ActorId,
            Action = l.Action,
            TargetType = l.TargetType,
            TargetId = l.TargetId,
            MetadataJson = l.MetadataJson,
            CreatedAt = l.CreatedAt
        }).ToList();
    }
}
