using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public sealed class SpecialPracticeRepository(AppDbContext db) : ISpecialPracticeRepository
{
    public Task<SpecialPracticeAttempt?> GetAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken) =>
        db.SpecialPracticeAttempts.FirstOrDefaultAsync(item => item.Id == attemptId && item.UserId == userId, cancellationToken);

    public Task<SpecialPracticeAttempt?> GetActiveAsync(Guid userId, Guid courseId, Guid contentId, CancellationToken cancellationToken) =>
        db.SpecialPracticeAttempts.Where(item => item.UserId == userId && item.CourseId == courseId &&
            item.ContentId == contentId && item.Status == "InProgress")
            .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public Task<List<SpecialPracticeAttempt>> GetForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.SpecialPracticeAttempts.Where(item => item.UserId == userId).OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<SpecialPracticeAttempt>> GetPendingAsync(CancellationToken cancellationToken) =>
        db.SpecialPracticeAttempts.Where(item => item.Status == "PendingReview")
            .OrderBy(item => item.SubmittedAt).ToListAsync(cancellationToken);

    public Task<SpecialPracticeAttempt?> GetForReviewAsync(Guid attemptId, CancellationToken cancellationToken) =>
        db.SpecialPracticeAttempts.FirstOrDefaultAsync(item => item.Id == attemptId, cancellationToken);

    public async Task SaveAsync(SpecialPracticeAttempt attempt, CancellationToken cancellationToken)
    {
        if (db.Entry(attempt).State == EntityState.Detached) db.SpecialPracticeAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);
    }
}
