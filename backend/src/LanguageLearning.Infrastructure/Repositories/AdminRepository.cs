using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _context;

    public AdminRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetUsersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(q) || u.DisplayName.ToLower().Contains(q));
        }

        return await query.OrderByDescending(u => u.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task RecordAuditLogAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        await _context.AuditLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
