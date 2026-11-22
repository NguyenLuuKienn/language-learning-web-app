using System.Text.Json;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public sealed class SkillPracticeService(
    ISkillPracticeRepository repository, ICourseRepository courses,
    IStudyAccessService access, ICurrentUserService currentUser,
    ILessonJourneyService journey) : ISkillPracticeService
{
    private static readonly Dictionary<string, string> Sections = new(StringComparer.OrdinalIgnoreCase)
    {
        ["listening"] = "listeningExercises", ["speaking"] = "speakingExercises",
        ["reading"] = "readingArticles", ["writing"] = "writingPrompts", ["grammar"] = "grammarTopics"
    };

    public async Task<List<SkillPracticeResponse>> GetAsync(Guid userId, string kind, CancellationToken cancellationToken)
    {
        ValidateKind(kind);
        return (await repository.GetForKindAsync(userId, kind.ToLowerInvariant(), cancellationToken))
            .Select(item => Map(item, null)).ToList();
    }

    public async Task<SkillPracticeResponse> RecordAsync(Guid userId, string kind, SkillPracticeRequest request, CancellationToken cancellationToken)
    {
        ValidateKind(kind);
        kind = kind.ToLowerInvariant();
        if (request.CourseId == Guid.Empty || string.IsNullOrWhiteSpace(request.ActivityId) || request.ActivityId.Length > 100)
            throw new ArgumentException(Messages.SkillPractice.InvalidActivity);
        if (!currentUser.IsAdmin() && !await access.CanAccessCourseAsync(userId, request.CourseId, cancellationToken))
            throw new NotFoundException(Messages.Course.CourseNotFound);
        var course = await courses.GetCourseByIdAsync(request.CourseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        var activity = FindActivity(course.MaterialJson, Sections[kind], request.ActivityId);
        if (activity == null) throw new NotFoundException(Messages.SkillPractice.InvalidActivity);

        bool? correct = null;
        if (kind is "listening" or "reading" or "grammar")
        {
            if (request.AnswerIndex.HasValue)
            {
                var question = kind == "reading" ? activity.Value.GetProperty("quiz") : activity.Value;
                if (!question.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array ||
                    request.AnswerIndex.Value < 0 || request.AnswerIndex.Value >= options.GetArrayLength())
                    throw new ArgumentException(Messages.SkillPractice.InvalidAnswer);
                correct = question.GetProperty("correctIndex").GetInt32() == request.AnswerIndex.Value;
            }
        }
        else if (request.AnswerIndex.HasValue) throw new ArgumentException(Messages.SkillPractice.InvalidAnswer);

        if (request.ClientAttemptId is { } clientAttemptId)
        {
            var attempt = new SkillPracticeEvent
            {
                Id = Guid.NewGuid(), UserId = userId, ClientAttemptId = clientAttemptId,
                CourseId = request.CourseId, Kind = kind, ActivityId = request.ActivityId,
                AnswerIndex = request.AnswerIndex, Practiced = request.Practiced, Correct = correct
            };
            var recorded = await repository.RecordAttemptAsync(attempt, cancellationToken);
            if (!recorded.Duplicate && (correct == true || request.Practiced))
            {
                var lessonId = FindLessonId(course.MaterialJson, Sections[kind], request.ActivityId);
                if (lessonId.HasValue) await journey.RefreshAsync(userId, lessonId.Value, cancellationToken);
            }
            return Map(recorded.Progress, recorded.Correct);
        }

        var progress = await repository.GetAsync(userId, request.CourseId, kind, request.ActivityId, cancellationToken)
            ?? new SkillPracticeProgress
            {
                Id = Guid.NewGuid(), UserId = userId, CourseId = request.CourseId,
                Kind = kind, ActivityId = request.ActivityId
            };
        if (correct.HasValue)
        {
            progress.Attempts++;
            if (correct.Value) { progress.CorrectAttempts++; progress.Status = "Completed"; }
        }
        else if (request.Practiced && (kind is "speaking" or "writing"))
        {
            progress.Attempts++;
            progress.Status = "Practiced";
        }
        progress.UpdatedAt = DateTime.UtcNow;
        await repository.SaveAsync(progress, cancellationToken);
        if (correct == true || request.Practiced)
        {
            var lessonId = FindLessonId(course.MaterialJson, Sections[kind], request.ActivityId);
            if (lessonId.HasValue) await journey.RefreshAsync(userId, lessonId.Value, cancellationToken);
        }
        return Map(progress, correct);
    }

    private static void ValidateKind(string kind)
    {
        if (!Sections.ContainsKey(kind)) throw new ArgumentException(Messages.SkillPractice.InvalidKind);
    }

    private static JsonElement? FindActivity(string? materialJson, string section, string activityId)
    {
        if (string.IsNullOrWhiteSpace(materialJson)) return null;
        using var document = JsonDocument.Parse(materialJson);
        if (!document.RootElement.TryGetProperty("units", out var units) || units.ValueKind != JsonValueKind.Array) return null;
        foreach (var unit in units.EnumerateArray())
        {
            foreach (var lesson in unit.GetProperty("lessons").EnumerateArray())
            {
                if (!lesson.TryGetProperty(section, out var activities) || activities.ValueKind != JsonValueKind.Array) continue;
                foreach (var activity in activities.EnumerateArray())
                    if (activity.TryGetProperty("id", out var id) && id.GetString() == activityId)
                        return activity.Clone();
            }
        }
        return null;
    }

    private static SkillPracticeResponse Map(SkillPracticeProgress progress, bool? correct) => new()
    {
        CourseId = progress.CourseId, ActivityId = progress.ActivityId, Status = progress.Status,
        Attempts = progress.Attempts, CorrectAttempts = progress.CorrectAttempts,
        Correct = correct, UpdatedAt = progress.UpdatedAt ?? progress.CreatedAt
    };

    private static Guid? FindLessonId(string? materialJson, string section, string activityId)
    {
        if (string.IsNullOrWhiteSpace(materialJson)) return null;
        using var document = JsonDocument.Parse(materialJson);
        if (!document.RootElement.TryGetProperty("units", out var units)) return null;
        foreach (var unit in units.EnumerateArray())
            foreach (var lesson in unit.GetProperty("lessons").EnumerateArray())
                if (lesson.TryGetProperty(section, out var activities) && activities.ValueKind == JsonValueKind.Array &&
                    activities.EnumerateArray().Any(item => item.TryGetProperty("id", out var id) && id.GetString() == activityId) &&
                    lesson.TryGetProperty("id", out var lessonId) && Guid.TryParse(lessonId.GetString(), out var parsed))
                    return parsed;
        return null;
    }
}
