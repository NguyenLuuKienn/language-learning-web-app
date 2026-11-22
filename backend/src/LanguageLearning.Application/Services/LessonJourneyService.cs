using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public sealed class LessonJourneyService(
    ICourseRepository courses, IStudyAccessService access, ICurrentUserService currentUser,
    IProgressRepository progressRepository, ILessonJourneyRepository journeyRepository,
    IVocabularySessionRepository vocabularySessions, ISkillPracticeRepository skillProgress,
    IWritingSubmissionRepository writingSubmissions, IQuizRepository quizzes,
    IUserRepository users) : ILessonJourneyService
{
    private static readonly (string Type, string Section, string Title, string Policy)[] PracticeSections =
    [
        ("vocabulary", "flashcards", "Từ vựng", "session_completed"),
        ("grammar", "grammarTopics", "Ngữ pháp", "all_correct"),
        ("listening", "listeningExercises", "Nghe", "all_correct"),
        ("speaking", "speakingExercises", "Nói", "practiced"),
        ("reading", "readingArticles", "Đọc", "all_correct"),
        ("writing", "writingPrompts", "Viết", "submission_required"),
        ("quiz", "quizQuestions", "Kiểm tra", "quiz_passed")
    ];

    public async Task<LessonJourneyResponse> GetAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken) =>
        await BuildAsync(userId, lessonId, false, cancellationToken);

    public async Task<LessonJourneyResponse> RefreshAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken) =>
        await BuildAsync(userId, lessonId, true, cancellationToken);

    public async Task<LessonJourneyResponse> VisitAsync(Guid userId, Guid lessonId, string stepId,
        SaveLessonJourneyStepRequest request, CancellationToken cancellationToken)
    {
        if (request.Cursor < 0 || request.Cursor > 10000 || request.DraftJson?.Length > 100000)
            throw new ArgumentException("Invalid lesson cursor or draft.");
        if (request.DraftJson != null)
        {
            using var _ = JsonDocument.Parse(request.DraftJson);
        }
        var journey = await BuildAsync(userId, lessonId, false, cancellationToken);
        if (request.ContentVersion != null && request.ContentVersion != journey.ContentVersion)
            throw new BusinessRuleException("LESSON_CONTENT_CHANGED", "This lesson content changed. Reload the lesson before saving progress.");
        var selected = journey.Steps.FirstOrDefault(item => item.Id == stepId)
            ?? throw new ArgumentException("This lesson step does not exist.");
        if (selected.Status == "Locked")
            throw new BusinessRuleException("LESSON_STEP_LOCKED", "Complete the previous lesson step first.");
        if (request.CompleteIntroduction && selected.Type is not ("introduction" or "recap"))
            throw new ArgumentException("Only reading steps can be acknowledged directly.");
        if (request.ActivityId != null && !selected.ActivityRefs.Contains(request.ActivityId))
            throw new ArgumentException("Activity is not part of this lesson step.");
        var states = await journeyRepository.GetStepsAsync(userId, lessonId, cancellationToken);
        var previous = states.FirstOrDefault(item => item.StepId == stepId);
        if (request.ExpectedRevision != (previous?.Revision ?? 0))
            throw new BusinessRuleException("LESSON_CHECKPOINT_CONFLICT", "This lesson step changed on another device. Reload its latest progress before saving.");
        var step = previous ?? new LessonStepProgress
        {
            Id = Guid.NewGuid(), UserId = userId, LessonId = lessonId, StepId = stepId
        };
        step.LastAccessedAt = DateTime.UtcNow;
        if (request.ActivityId != null) step.ActivityId = request.ActivityId;
        step.Cursor = request.Cursor;
        if (request.DraftJson != null) step.DraftJson = request.DraftJson;
        if (request.CompleteIntroduction) { step.Status = "Completed"; step.CompletedAt ??= DateTime.UtcNow; }
        await courses.EnrollAsync(userId, journey.CourseId, cancellationToken);
        await journeyRepository.SaveStepAsync(step, cancellationToken);
        var progress = await progressRepository.GetProgressAsync(userId, lessonId, cancellationToken)
            ?? new LessonProgress { Id = Guid.NewGuid(), UserId = userId, LessonId = lessonId };
        progress.CurrentStepId = stepId;
        progress.LastAccessedAt = DateTime.UtcNow;
        if (progress.Status != "Completed") progress.Status = "InProgress";
        await progressRepository.UpsertProgressAsync(progress, cancellationToken);
        return await BuildAsync(userId, lessonId, true, cancellationToken);
    }

    private async Task<LessonJourneyResponse> BuildAsync(Guid userId, Guid lessonId, bool persist,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin() && !await access.CanAccessLessonAsync(userId, lessonId, cancellationToken))
            throw new BusinessRuleException("LESSON_NOT_ACCESSIBLE", "Lesson is not available for the current goal.");
        var lesson = await courses.GetLessonByIdAsync(lessonId, cancellationToken: cancellationToken)
            ?? throw new ArgumentException("Lesson was not found.");
        var course = lesson.Unit.Course;
        if (!currentUser.IsAdmin())
        {
            var orderedCourse = await courses.GetCourseByIdAsync(course.Id, true, cancellationToken);
            var orderedLessons = orderedCourse?.Units.OrderBy(item => item.SortOrder)
                .SelectMany(item => item.Lessons.Where(child => child.Status == LanguageLearning.Domain.Enums.ContentStatus.Published)
                    .OrderBy(child => child.SortOrder)).ToList() ?? [];
            var index = orderedLessons.FindIndex(item => item.Id == lessonId);
            if (index > 0)
            {
                var previous = await progressRepository.GetProgressAsync(userId, orderedLessons[index - 1].Id, cancellationToken);
                var existing = await progressRepository.GetProgressAsync(userId, lessonId, cancellationToken);
                if (previous?.Status != "Completed" && existing?.Status is not ("InProgress" or "Completed"))
                    throw new BusinessRuleException("LESSON_LOCKED", "Complete the previous lesson first.");
            }
        }
        if (string.IsNullOrWhiteSpace(course.MaterialJson))
            throw new BusinessRuleException("LESSON_MATERIAL_MISSING", "Lesson material is unavailable.");
        using var document = JsonDocument.Parse(course.MaterialJson);
        var materialLesson = FindMaterialLesson(document.RootElement, lessonId)
            ?? throw new BusinessRuleException("LESSON_MATERIAL_MISSING", "Lesson material is unavailable.");
        var definitions = DefineSteps(materialLesson);
        if (definitions.Count == 0)
            throw new BusinessRuleException("LESSON_STEPS_MISSING", "Lesson steps are unavailable.");
        var saved = await journeyRepository.GetStepsAsync(userId, lessonId, cancellationToken);
        var progress = await progressRepository.GetProgressAsync(userId, lessonId, cancellationToken);
        var contentVersion = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(materialLesson.GetRawText())));
        var session = await journeyRepository.GetSessionAsync(userId, lessonId, cancellationToken);
        var output = new LessonJourneyResponse
        {
            CourseId = course.Id, UnitId = lesson.UnitId, LessonId = lessonId,
            CourseTitle = course.Title, UnitTitle = lesson.Unit.Title, LessonTitle = lesson.Title,
            Summary = Text(materialLesson, "summary") ?? string.Empty,
            Status = progress?.Status ?? "NotStarted",
            ContentVersion = contentVersion,
            ContentChangedSinceStart = session != null && session.ContentVersion != contentVersion
        };
        var firstIncompleteSeen = false;
        foreach (var definition in definitions)
        {
            var record = saved.FirstOrDefault(item => item.StepId == definition.Id);
            var completed = await IsCompleteAsync(userId, course.Id, lessonId, materialLesson,
                definition, record, cancellationToken);
            definition.Status = completed ? "Completed" : firstIncompleteSeen ? "Locked" :
                record != null ? "InProgress" : "Available";
            if (!completed && definition.Required) firstIncompleteSeen = true;
            definition.ActivityId = record?.ActivityId;
            definition.Cursor = record?.Cursor ?? 0;
            definition.Revision = record?.Revision ?? 0;
            definition.DraftJson = record?.DraftJson ?? "{}";
            output.Steps.Add(definition);
        }
        output.TotalRequiredSteps = output.Steps.Count(item => item.Required);
        output.CompletedRequiredSteps = output.Steps.Count(item => item.Required && item.Status == "Completed");
        output.CurrentStepId = output.Steps.FirstOrDefault(item => item.Required && item.Status != "Completed")?.Id
            ?? output.Steps.FirstOrDefault(item => item.Status != "Completed")?.Id
            ?? output.Steps.Last().Id;
        var skillActivities = output.Steps.Where(item => item.Type is "grammar" or "listening" or "reading")
            .SelectMany(item => item.ActivityRefs.Select(id => (Kind: item.Type, ActivityId: id))).ToArray();
        output.WeakPoints = (await journeyRepository.GetWeakPointsAsync(userId, course.Id, lessonId,
            skillActivities, cancellationToken))
            .Where(item => output.Steps.Any(step => step.Type == item.StepType &&
                (step.Type == "quiz" || step.ActivityRefs.Contains(item.ActivityId))))
            .OrderByDescending(item => item.IncorrectAttempts).Take(5).ToList();
        foreach (var weakPoint in output.WeakPoints.Where(item => item.StepType != "quiz"))
        {
            var section = PracticeSections.First(item => item.Type == weakPoint.StepType).Section;
            var activity = Array(materialLesson, section).FirstOrDefault(item => Text(item, "id") == weakPoint.ActivityId);
            weakPoint.Prompt = Text(activity, "title") ?? Text(activity, "question") ??
                Text(activity, "prompt") ?? weakPoint.StepType;
        }
        if (persist && output.TotalRequiredSteps > 0 && output.CompletedRequiredSteps == output.TotalRequiredSteps &&
            progress?.Status != "Completed")
        {
            progress ??= new LessonProgress { Id = Guid.NewGuid(), UserId = userId, LessonId = lessonId };
            progress.Status = "Completed";
            progress.CompletedAt = DateTime.UtcNow;
            progress.CurrentStepId = null;
            progress.LastAccessedAt = DateTime.UtcNow;
            await progressRepository.UpsertProgressAsync(progress, cancellationToken);
            var user = await users.GetByIdAsync(userId, cancellationToken);
            await progressRepository.RecordLessonCompletionActivityAsync(userId,
                user?.Timezone ?? "Asia/Ho_Chi_Minh", cancellationToken);
            output.Status = "Completed";
        }
        else if (persist && progress?.Status == "InProgress" && progress.CurrentStepId != output.CurrentStepId)
        {
            progress.CurrentStepId = output.CurrentStepId;
            await progressRepository.UpsertProgressAsync(progress, cancellationToken);
        }
        if (persist && progress != null)
            await journeyRepository.RecordJourneyAsync(userId, lessonId, contentVersion,
                output.Status == "Completed",
                output.Steps.Where(item => item.Status == "Completed")
                    .Select(item => (item.Id, item.Type)).ToArray(), cancellationToken);
        return output;
    }

    private async Task<bool> IsCompleteAsync(Guid userId, Guid courseId, Guid lessonId, JsonElement material,
        LessonJourneyStepResponse step, LessonStepProgress? record, CancellationToken cancellationToken)
    {
        if (step.Type is "introduction" or "recap") return record?.Status == "Completed";
        if (step.Type == "vocabulary")
        {
            var words = Array(material, "flashcards").Select(item => Text(item, "front")?.Trim().ToLowerInvariant())
                .Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToHashSet();
            if (words.Count == 0) return false;
            var sessions = await vocabularySessions.GetCompletedByLessonsAsync(userId, [lessonId], cancellationToken);
            var introduced = new HashSet<string>();
            foreach (var session in sessions)
            {
                try
                {
                    using var steps = JsonDocument.Parse(session.StepsJson);
                    foreach (var item in steps.RootElement.EnumerateArray())
                        if (Text(item, "Kind", "kind") == "introduction" && Text(item, "Word", "word") is { } word)
                            introduced.Add(word.Trim().ToLowerInvariant());
                }
                catch (JsonException) { }
            }
            return words.All(introduced.Contains);
        }
        if (step.Type == "quiz")
        {
            var attempts = await quizzes.GetUserAttemptsAsync(userId, lessonId, 50, cancellationToken);
            return attempts.Any(item => item.MaxScore > 0 && (double)item.Score / item.MaxScore * 100 >= 70);
        }
        if (step.Type == "writing")
        {
            foreach (var id in step.ActivityRefs)
                if (await writingSubmissions.GetLatestAsync(userId, id, cancellationToken) == null) return false;
            return step.ActivityRefs.Count > 0;
        }
        if (step.Type is "grammar" or "listening" or "speaking" or "reading")
        {
            foreach (var id in step.ActivityRefs)
            {
                var state = await skillProgress.GetAsync(userId, courseId, step.Type, id, cancellationToken);
                if (state == null || (step.Type == "speaking"
                    ? state.Status is not ("Practiced" or "Completed")
                    : state.Status != "Completed")) return false;
            }
            return step.ActivityRefs.Count > 0;
        }
        return false;
    }

    private static List<LessonJourneyStepResponse> DefineSteps(JsonElement lesson)
    {
        if (lesson.TryGetProperty("lessonSteps", out var configured) && configured.ValueKind == JsonValueKind.Array)
        {
            if (configured.GetArrayLength() == 0)
                throw new BusinessRuleException("LESSON_STEP_INVALID", "Lesson steps cannot be empty.");
            var result = new List<LessonJourneyStepResponse>();
            foreach (var item in configured.EnumerateArray())
            {
                var type = Text(item, "type") ?? string.Empty;
                if (type is not ("introduction" or "recap") && !PracticeSections.Any(part => part.Type == type))
                    throw new BusinessRuleException("LESSON_STEP_INVALID", "Lesson step type is invalid.");
                var section = PracticeSections.FirstOrDefault(part => part.Type == type);
                var refs = Array(item, "activityRefs").Where(value => value.ValueKind == JsonValueKind.String)
                    .Select(value => value.GetString() ?? string.Empty).ToList();
                var actualIds = section.Section == null ? [] : Array(lesson, section.Section)
                    .Select(value => Text(value, "id")).Where(value => value != null).ToList();
                if (refs.Any(id => !actualIds.Contains(id)) ||
                    (type != "introduction" && type != "recap" && refs.Count == 0 && type != "quiz" && type != "vocabulary"))
                    throw new BusinessRuleException("LESSON_STEP_INVALID", "Lesson step references unavailable content.");
                if (type == "vocabulary" && !Array(lesson, "flashcards").Any() ||
                    type == "quiz" && !Array(lesson, "quizQuestions").Any())
                    throw new BusinessRuleException("LESSON_STEP_INVALID", "Lesson step has no content.");
                var policy = item.TryGetProperty("completionPolicy", out var configuredPolicy)
                    ? Text(configuredPolicy, "kind") : null;
                if (policy != null && policy != (section.Policy ?? "viewed"))
                    throw new BusinessRuleException("LESSON_STEP_INVALID", "Lesson step completion policy is invalid.");
                if (refs.Count == 0 && type is "vocabulary" or "quiz")
                    refs = actualIds.Select(id => id!).ToList();
                result.Add(new LessonJourneyStepResponse
                {
                    Id = Text(item, "id") ?? type,
                    Type = type, Title = Text(item, "title") ?? section.Title ?? type,
                    Order = item.TryGetProperty("order", out var order) && order.TryGetInt32(out var value) ? value : result.Count,
                    Required = !item.TryGetProperty("required", out var required) || required.ValueKind != JsonValueKind.False,
                    CompletionPolicy = policy ?? section.Policy ?? "viewed",
                    ActivityRefs = refs
                });
            }
            if (result.GroupBy(item => item.Id).Any(group => group.Count() > 1))
                throw new BusinessRuleException("LESSON_STEP_INVALID", "Lesson step IDs must be unique.");
            if (!result.Any(item => item.Type == "introduction" && item.Required))
                throw new BusinessRuleException("LESSON_STEP_INVALID", "A required introduction step is needed.");
            if (PracticeSections.Any(section => Array(lesson, section.Section).Any() &&
                !result.Any(step => step.Type == section.Type)))
                throw new BusinessRuleException("LESSON_STEP_INVALID", "Lesson steps omit existing content.");
            return result.OrderBy(item => item.Order).ToList();
        }
        var inferred = new List<LessonJourneyStepResponse>
        {
            new() { Id = "introduction", Type = "introduction", Title = "Giới thiệu", Order = 0,
                CompletionPolicy = "viewed" }
        };
        foreach (var section in PracticeSections)
        {
            var ids = Array(lesson, section.Section).Select(item => Text(item, "id"))
                .Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToList();
            if (ids.Count == 0) continue;
            inferred.Add(new LessonJourneyStepResponse
            {
                Id = section.Type, Type = section.Type, Title = section.Title, Order = inferred.Count,
                CompletionPolicy = section.Policy, ActivityRefs = ids
            });
        }
        return inferred;
    }

    private static JsonElement? FindMaterialLesson(JsonElement root, Guid lessonId)
    {
        foreach (var unit in Array(root, "units"))
            foreach (var lesson in Array(unit, "lessons"))
                if (Text(lesson, "id") == lessonId.ToString()) return lesson;
        return null;
    }

    private static IEnumerable<JsonElement> Array(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [];

    private static string? Text(JsonElement element, params string[] names)
    {
        foreach (var name in names)
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }
}
