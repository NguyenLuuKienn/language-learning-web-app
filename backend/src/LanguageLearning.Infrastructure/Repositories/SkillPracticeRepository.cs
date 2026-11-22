using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using LanguageLearning.Application.Common.Exceptions;
using System.Data;

namespace LanguageLearning.Infrastructure.Repositories;

public sealed class SkillPracticeRepository(AppDbContext context) : ISkillPracticeRepository
{
    public async Task<(SkillPracticeProgress Progress, bool? Correct, bool Duplicate)> RecordAttemptAsync(
        SkillPracticeEvent attempt, CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                context.ChangeTracker.Clear();
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var existingEvent = await context.SkillPracticeEvents.AsNoTracking().FirstOrDefaultAsync(item =>
                    item.UserId == attempt.UserId && item.ClientAttemptId == attempt.ClientAttemptId, cancellationToken);
                if (existingEvent != null)
                {
                    ValidateRetry(existingEvent, attempt);
                    var prior = await GetAsync(attempt.UserId, attempt.CourseId, attempt.Kind, attempt.ActivityId, cancellationToken)
                        ?? throw new BusinessRuleException("SKILL_ATTEMPT_CONFLICT", "The saved skill attempt has no progress record.");
                    return (prior, existingEvent.Correct, true);
                }
                var progress = await GetAsync(attempt.UserId, attempt.CourseId, attempt.Kind, attempt.ActivityId, cancellationToken)
                    ?? new SkillPracticeProgress
                    {
                        Id = Guid.NewGuid(), UserId = attempt.UserId, CourseId = attempt.CourseId,
                        Kind = attempt.Kind, ActivityId = attempt.ActivityId
                    };
                if (attempt.Correct.HasValue)
                {
                    progress.Attempts++;
                    if (attempt.Correct.Value) { progress.CorrectAttempts++; progress.Status = "Completed"; }
                }
                else if (attempt.Practiced && attempt.Kind is ("speaking" or "writing"))
                {
                    progress.Attempts++;
                    progress.Status = "Practiced";
                }
                progress.UpdatedAt = DateTime.UtcNow;
                if (context.Entry(progress).State == EntityState.Detached) context.SkillPracticeProgresses.Add(progress);
                context.SkillPracticeEvents.Add(attempt);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return (progress, attempt.Correct, false);
            });
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            var existingEvent = await context.SkillPracticeEvents.AsNoTracking().FirstOrDefaultAsync(item =>
                item.UserId == attempt.UserId && item.ClientAttemptId == attempt.ClientAttemptId, cancellationToken);
            if (existingEvent == null) throw;
            ValidateRetry(existingEvent, attempt);
            var progress = await GetAsync(attempt.UserId, attempt.CourseId, attempt.Kind, attempt.ActivityId, cancellationToken)
                ?? throw new BusinessRuleException("SKILL_ATTEMPT_CONFLICT", "The saved skill attempt has no progress record.");
            return (progress, existingEvent.Correct, true);
        }
    }

    private static void ValidateRetry(SkillPracticeEvent saved, SkillPracticeEvent attempt)
    {
        if (saved.CourseId != attempt.CourseId || saved.Kind != attempt.Kind ||
            saved.ActivityId != attempt.ActivityId || saved.AnswerIndex != attempt.AnswerIndex ||
            saved.Practiced != attempt.Practiced)
            throw new BusinessRuleException("SKILL_ATTEMPT_CONFLICT", "This attempt identifier was already used for another answer.");
    }

    public Task<List<SkillPracticeProgress>> GetForKindAsync(Guid userId, string kind, CancellationToken cancellationToken) =>
        context.SkillPracticeProgresses.AsNoTracking()
            .Where(item => item.UserId == userId && item.Kind == kind)
            .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt).ToListAsync(cancellationToken);

    public Task<SkillPracticeProgress?> GetAsync(Guid userId, Guid courseId, string kind, string activityId, CancellationToken cancellationToken) =>
        context.SkillPracticeProgresses.FirstOrDefaultAsync(item =>
            item.UserId == userId && item.CourseId == courseId && item.Kind == kind && item.ActivityId == activityId, cancellationToken);

    public async Task SaveAsync(SkillPracticeProgress progress, CancellationToken cancellationToken)
    {
        if (context.Entry(progress).State == EntityState.Detached) context.SkillPracticeProgresses.Add(progress);
        await context.SaveChangesAsync(cancellationToken);
    }
}
