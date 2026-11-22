using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class WritingSubmissionRepository : IWritingSubmissionRepository
{
    private readonly AppDbContext _context;

    public WritingSubmissionRepository(AppDbContext context) => _context = context;

    public Task<WritingSubmission?> GetLatestAsync(Guid userId, string promptId, CancellationToken cancellationToken = default) =>
        _context.WritingSubmissions.AsNoTracking()
            .Where(submission => submission.UserId == userId && submission.PromptId == promptId)
            .OrderByDescending(submission => submission.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<WritingSubmission> AddAsync(WritingSubmission submission, CancellationToken cancellationToken = default)
    {
        await _context.WritingSubmissions.AddAsync(submission, cancellationToken);
        try { await _context.SaveChangesAsync(cancellationToken); return submission; }
        catch (DbUpdateException) when (submission.ClientSubmissionId.HasValue)
        {
            _context.ChangeTracker.Clear();
            var existing = await _context.WritingSubmissions.AsNoTracking().FirstOrDefaultAsync(item =>
                item.UserId == submission.UserId && item.ClientSubmissionId == submission.ClientSubmissionId, cancellationToken);
            if (existing == null) throw;
            return existing;
        }
    }
}
