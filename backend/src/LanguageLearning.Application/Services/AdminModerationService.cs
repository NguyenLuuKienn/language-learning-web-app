using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public record AdminUserResponse(Guid Id, string Email, string DisplayName, string Role, bool IsActive);
public record AdminDeckResponse(Guid Id, string Title, Guid OwnerId, bool IsPublic, bool IsModerated);

public class AdminModerationService(IAdminModerationRepository repository, ICurrentUserService currentUser)
{
    public async Task<PagedResponse<AdminUserResponse>> ListUsersAsync(PaginationParams pagination, string? search, CancellationToken ct)
    {
        var page = await repository.ListUsersAsync(pagination, search, ct);
        return new(page.Items.Select(Map).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<PagedResponse<AdminDeckResponse>> ListDecksAsync(PaginationParams pagination, string? search, CancellationToken ct)
    {
        var page = await repository.ListDecksAsync(pagination, search, ct);
        return new(page.Items.Select(Map).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<AdminUserResponse> SetUserActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var actorId = currentUser.GetRequiredUserId();
        if (id == actorId && !active) throw new ConflictException("An admin cannot lock their own account.");
        var user = await repository.FindUserAsync(id, ct) ?? throw new NotFoundException("User not found.");
        var unchanged = user.IsActive == active;
        user.IsActive = active;
        await repository.SaveWithAuditAsync(new AuditLog
        {
            ActorId = actorId,
            Action = active ? "UserUnlocked" : "UserLocked",
            TargetType = "User",
            TargetId = id,
            MetadataJson = unchanged ? "{\"unchanged\":true}" : "{}"
        }, ct);
        return Map(user);
    }

    public async Task<AdminDeckResponse> SetDeckHiddenAsync(Guid id, bool hidden, CancellationToken ct)
    {
        var deck = await repository.FindDeckAsync(id, ct) ?? throw new NotFoundException("Deck not found.");
        if (hidden && !deck.IsPublic && !deck.IsModerated)
            throw new ConflictException("Only public decks can be hidden.");
        if (!hidden && !deck.IsPublic && !deck.IsModerated)
            throw new ConflictException("This deck was not hidden by moderation.");
        var unchanged = deck.IsModerated == hidden;
        deck.IsModerated = hidden;
        deck.IsPublic = !hidden;
        await repository.SaveWithAuditAsync(new AuditLog
        {
            ActorId = currentUser.GetRequiredUserId(),
            Action = hidden ? "DeckHidden" : "DeckRestored",
            TargetType = "Deck",
            TargetId = id,
            MetadataJson = unchanged ? "{\"unchanged\":true}" : "{}"
        }, ct);
        return Map(deck);
    }

    private static AdminUserResponse Map(User user) => new(user.Id, user.Email, user.DisplayName, user.Role.ToString(), user.IsActive);
    private static AdminDeckResponse Map(Deck deck) => new(deck.Id, deck.Title, deck.OwnerId, deck.IsPublic, deck.IsModerated);
}
