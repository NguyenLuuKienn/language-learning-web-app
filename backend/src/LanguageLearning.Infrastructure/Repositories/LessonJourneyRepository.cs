using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Progress;
using System.Text.Json;

namespace LanguageLearning.Infrastructure.Repositories;

public sealed class LessonJourneyRepository(AppDbContext context) : ILessonJourneyRepository
{
    public Task<LearningSession?> GetSessionAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken) =>
        context.LearningSessions.AsNoTracking().FirstOrDefaultAsync(item =>
            item.UserId == userId && item.LessonId == lessonId, cancellationToken);

    public async Task<bool> HasLearnerStateAsync(Guid lessonId, CancellationToken cancellationToken) =>
        await context.LearningSessions.AnyAsync(item => item.LessonId == lessonId, cancellationToken) ||
        await context.LessonStepProgresses.AnyAsync(item => item.LessonId == lessonId, cancellationToken) ||
        await context.LessonProgresses.AnyAsync(item => item.LessonId == lessonId, cancellationToken) ||
        await context.QuizAttempts.AnyAsync(item => item.LessonId == lessonId, cancellationToken) ||
        await context.VocabularySessions.AnyAsync(item => item.LessonId == lessonId, cancellationToken) ||
        await context.CardLessonSources.AnyAsync(item => item.LessonId == lessonId, cancellationToken);

    public async Task<List<LearningWeakPointResponse>> GetWeakPointsAsync(Guid userId, Guid courseId, Guid lessonId,
        IReadOnlyCollection<(string Kind, string ActivityId)> skillActivities, CancellationToken cancellationToken)
    {
        var result = new List<LearningWeakPointResponse>();
        var quizAnswers = await context.QuizAnswers.AsNoTracking()
            .Where(item => item.Attempt.UserId == userId && item.Attempt.LessonId == lessonId)
            .Select(item => new { item.QuestionId, item.Question.Prompt, item.Question.ConfigJson,
                item.IsCorrect, item.Attempt.CompletedAt, item.CreatedAt })
            .ToListAsync(cancellationToken);
        foreach (var group in quizAnswers.GroupBy(item => item.QuestionId))
        {
            var latest = group.OrderByDescending(item => item.CompletedAt).ThenByDescending(item => item.CreatedAt).First();
            if (latest.IsCorrect) continue;
            var activityId = latest.QuestionId.ToString();
            try
            {
                using var config = JsonDocument.Parse(latest.ConfigJson);
                if (config.RootElement.TryGetProperty("materialId", out var id) && id.ValueKind == JsonValueKind.String)
                    activityId = id.GetString() ?? activityId;
            }
            catch (JsonException) { }
            result.Add(new LearningWeakPointResponse
            {
                StepType = "quiz", ActivityId = activityId, Prompt = latest.Prompt,
                IncorrectAttempts = group.Count(item => !item.IsCorrect)
            });
        }
        if (skillActivities.Count == 0) return result;
        var ids = skillActivities.Select(item => item.ActivityId).Distinct().ToArray();
        var events = await context.SkillPracticeEvents.AsNoTracking()
            .Where(item => item.UserId == userId && item.CourseId == courseId && ids.Contains(item.ActivityId) && item.Correct != null)
            .Select(item => new { item.Kind, item.ActivityId, item.Correct, item.CreatedAt })
            .ToListAsync(cancellationToken);
        var allowed = skillActivities.ToHashSet();
        foreach (var group in events.GroupBy(item => (item.Kind, item.ActivityId)))
        {
            if (!allowed.Contains(group.Key) || group.OrderByDescending(item => item.CreatedAt).First().Correct != false) continue;
            result.Add(new LearningWeakPointResponse
            {
                StepType = group.Key.Kind, ActivityId = group.Key.ActivityId,
                IncorrectAttempts = group.Count(item => item.Correct == false)
            });
        }
        return result;
    }

    public async Task RecordJourneyAsync(Guid userId, Guid lessonId, string contentVersion, bool completed,
        IReadOnlyCollection<(string StepId, string StepType)> completedSteps, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var session = await context.LearningSessions.FirstOrDefaultAsync(item =>
            item.UserId == userId && item.LessonId == lessonId, cancellationToken);
        if (session == null)
            context.LearningSessions.Add(new LearningSession
            {
                Id = Guid.NewGuid(), UserId = userId, LessonId = lessonId,
                ContentVersion = contentVersion, StartedAt = now, LastActiveAt = now,
                CompletedAt = completed ? now : null
            });
        else
        {
            session.LastActiveAt = now;
            if (completed) session.CompletedAt ??= now;
        }

        var existing = (await context.LearningEvidences.AsNoTracking()
            .Where(item => item.UserId == userId && item.LessonId == lessonId)
            .Select(item => item.StepId).ToListAsync(cancellationToken)).ToHashSet();
        foreach (var step in completedSteps)
            if (existing.Add(step.StepId))
                context.LearningEvidences.Add(new LearningEvidence
                {
                    Id = Guid.NewGuid(), UserId = userId, LessonId = lessonId,
                    StepId = step.StepId, StepType = step.StepType,
                    Outcome = step.StepType == "speaking" ? "Practiced" : "Completed",
                    ContentVersion = contentVersion, ObservedAt = now
                });
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<LessonStepProgress>> GetStepsAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken) =>
        context.LessonStepProgresses.Where(item => item.UserId == userId && item.LessonId == lessonId)
            .ToListAsync(cancellationToken);

    public async Task SaveStepAsync(LessonStepProgress step, CancellationToken cancellationToken)
    {
        var existing = await context.LessonStepProgresses.FirstOrDefaultAsync(item =>
            item.UserId == step.UserId && item.LessonId == step.LessonId && item.StepId == step.StepId, cancellationToken);
        if (existing == null) { step.Revision = 1; context.LessonStepProgresses.Add(step); }
        else
        {
            existing.Status = step.Status;
            existing.ActivityId = step.ActivityId;
            existing.Cursor = step.Cursor;
            existing.DraftJson = step.DraftJson;
            existing.LastAccessedAt = step.LastAccessedAt;
            existing.CompletedAt = step.CompletedAt;
            existing.Revision++;
        }
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleException("LESSON_CHECKPOINT_CONFLICT", "This lesson step changed on another device. Reload its latest progress before saving.");
        }
    }
}
