using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public sealed class NotebookRepository : INotebookRepository
{
    private readonly AppDbContext _context;
    public NotebookRepository(AppDbContext context) => _context = context;

    public async Task<(List<NotebookEntry> Items, int Total)> GetPageAsync(
        Guid userId, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.NotebookEntries.AsNoTracking().Where(entry => entry.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(entry => entry.Text.Contains(search) || entry.Note.Contains(search));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(entry => entry.CreatedAt).ThenByDescending(entry => entry.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<NotebookEntry?> GetByIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken) =>
        _context.NotebookEntries.FirstOrDefaultAsync(entry => entry.UserId == userId && entry.Id == entryId, cancellationToken);

    public async Task SaveAsync(NotebookEntry entry, CancellationToken cancellationToken)
    {
        if (_context.Entry(entry).State == EntityState.Detached) _context.NotebookEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(NotebookEntry entry, CancellationToken cancellationToken)
    {
        _context.NotebookEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
