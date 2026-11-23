using LanguageLearning.Application.Common;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class AdminModerationRepository(AppDbContext db) : IAdminModerationRepository
{
    public async Task<PagedResponse<User>> ListUsersAsync(PaginationParams pagination, string? search, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(value => value.Email.Contains(search.Trim()) || value.DisplayName.Contains(search.Trim()));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(value => value.Email).ThenBy(value => value.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToListAsync(ct);
        return new(items, pagination.Page, pagination.PageSize, total);
    }

    public async Task<PagedResponse<Deck>> ListDecksAsync(PaginationParams pagination, string? search, CancellationToken ct)
    {
        var query = db.Decks.AsNoTracking().Where(value => !value.IsDeleted && (value.IsPublic || value.IsModerated));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(value => value.Title.Contains(search.Trim()));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(value => value.CreatedAt).ThenBy(value => value.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToListAsync(ct);
        return new(items, pagination.Page, pagination.PageSize, total);
    }

    public Task<User?> FindUserAsync(Guid id, CancellationToken ct) => db.Users.FirstOrDefaultAsync(value => value.Id == id, ct);
    public Task<Deck?> FindDeckAsync(Guid id, CancellationToken ct) => db.Decks.FirstOrDefaultAsync(value => value.Id == id && !value.IsDeleted, ct);

    public async Task SaveWithAuditAsync(AuditLog audit, CancellationToken ct)
    {
        db.AuditLogs.Add(audit);
        await db.SaveChangesAsync(ct);
    }
}
