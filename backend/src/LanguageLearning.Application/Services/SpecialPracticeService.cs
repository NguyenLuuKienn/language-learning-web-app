using System.Text.Json;
using System.Text.Json.Nodes;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public sealed class SpecialPracticeService(
    ISpecialPracticeRepository attempts, ICourseRepository courses,
    IStudyAccessService access, ICurrentUserService currentUser) : ISpecialPracticeService
{
    public async Task<List<SpecialPracticeCatalogItem>> GetCatalogAsync(Guid userId, CancellationToken cancellationToken)
    {
        var target = currentUser.IsAdmin() ? null : await access.GetTargetAsync(userId, cancellationToken);
        if (!currentUser.IsAdmin() && target == null) return [];
        var summaries = await courses.GetCourseSummariesAsync(target?.LanguageId, target?.Level, null, userId, cancellationToken);
        var history = await attempts.GetForUserAsync(userId, cancellationToken);
        foreach (var expired in history.Where(item => item.Status == "InProgress" && IsExpired(item)))
            await FinalizeAsync(expired, true, cancellationToken);
        var catalog = new List<SpecialPracticeCatalogItem>();
        foreach (var summary in summaries)
        {
            if (!currentUser.IsAdmin() && !await access.CanAccessCourseAsync(userId, summary.Id, cancellationToken)) continue;
            var course = await courses.GetCourseByIdAsync(summary.Id, cancellationToken: cancellationToken);
            if (course?.Status != ContentStatus.Published || string.IsNullOrWhiteSpace(course.MaterialJson)) continue;
            using var document = JsonDocument.Parse(course.MaterialJson);
            foreach (var kind in new[] { "certificate", "interview" })
                foreach (var content in GetModules(document.RootElement, kind))
                {
                    if (!Guid.TryParse(Read(content, "id"), out var contentId)) continue;
                    if (kind == "interview" && !currentUser.IsAdmin() &&
                        !MatchesProfession(target, Read(content, "profession"))) continue;
                    var previous = history.Where(item => item.CourseId == course.Id && item.ContentId == contentId)
                        .OrderByDescending(item => item.CreatedAt).FirstOrDefault();
                    catalog.Add(new SpecialPracticeCatalogItem(course.Id, contentId, kind,
                        Read(content, "title"), course.Language.Code, course.Level, course.Domain,
                        Read(content, "certificate"), Read(content, "profession"), Questions(content, kind).Count,
                        previous?.Status == "InProgress" ? previous.Id : null, previous?.Id, previous?.Status));
                }
        }
        foreach (var previous in history.GroupBy(item => (item.CourseId, item.ContentId))
            .Select(group => group.OrderByDescending(item => item.CreatedAt).First())
            .Where(item => catalog.All(entry => entry.CourseId != item.CourseId || entry.ContentId != item.ContentId)))
        {
            var course = await courses.GetCourseByIdAsync(previous.CourseId, cancellationToken: cancellationToken);
            if (course?.Status != ContentStatus.Published ||
                !currentUser.IsAdmin() && !await access.CanAccessCourseAsync(userId, course.Id, cancellationToken)) continue;
            using var snapshot = JsonDocument.Parse(previous.ContentSnapshotJson);
            var content = snapshot.RootElement;
            catalog.Add(new SpecialPracticeCatalogItem(course.Id, previous.ContentId, previous.Kind,
                Read(content, "title"), course.Language.Code, course.Level, course.Domain,
                Read(content, "certificate"), Read(content, "profession"), Questions(content, previous.Kind).Count,
                previous.Status == "InProgress" ? previous.Id : null, previous.Id, previous.Status, true));
        }
        return catalog.OrderBy(item => item.Kind).ThenBy(item => item.Certificate)
            .ThenBy(item => item.Profession).ThenBy(item => item.Title).ToList();
    }

    public async Task<SpecialPracticeAttemptResponse> StartAsync(Guid userId, StartSpecialPracticeRequest request, CancellationToken cancellationToken)
    {
        ValidateKind(request.Kind);
        var course = await GetAccessibleCourseAsync(userId, request.CourseId, cancellationToken);
        var existing = await attempts.GetActiveAsync(userId, course.Id, request.ContentId, cancellationToken);
        if (existing != null)
        {
            if (IsExpired(existing)) await FinalizeAsync(existing, true, cancellationToken);
            else return Map(existing);
        }
        using var document = JsonDocument.Parse(course.MaterialJson ?? "{}");
        var content = GetModules(document.RootElement, request.Kind)
            .FirstOrDefault(item => Guid.TryParse(Read(item, "id"), out var id) && id == request.ContentId);
        if (content.ValueKind != JsonValueKind.Object) throw new NotFoundException("Practice content was not found.");
        if (request.Kind == "interview" && !currentUser.IsAdmin() &&
            !MatchesProfession(await access.GetTargetAsync(userId, cancellationToken), Read(content, "profession")))
            throw new NotFoundException("Practice content was not found.");
        var attempt = new SpecialPracticeAttempt
        {
            Id = Guid.NewGuid(), UserId = userId, CourseId = course.Id, ContentId = request.ContentId,
            Kind = request.Kind, ContentSnapshotJson = content.GetRawText(), AnswersJson = "{}"
        };
        await attempts.SaveAsync(attempt, cancellationToken);
        return Map(attempt);
    }

    public async Task<SpecialPracticeAttemptResponse> GetAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, cancellationToken);
        if (attempt.Status == "InProgress" && IsExpired(attempt)) await FinalizeAsync(attempt, true, cancellationToken);
        return Map(attempt);
    }

    public async Task<SpecialPracticeAttemptResponse> SaveAnswerAsync(Guid userId, Guid attemptId,
        SaveSpecialPracticeAnswerRequest request, CancellationToken cancellationToken)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, cancellationToken);
        if (attempt.Status != "InProgress") throw new ArgumentException("This practice attempt has already been submitted.");
        if (IsExpired(attempt))
        {
            await FinalizeAsync(attempt, true, cancellationToken);
            throw new ArgumentException("Time is up. The attempt has been submitted.");
        }
        using var document = JsonDocument.Parse(attempt.ContentSnapshotJson);
        var question = Questions(document.RootElement, attempt.Kind).FirstOrDefault(item => item.Id == request.QuestionId);
        if (question == null) throw new NotFoundException("Practice question was not found.");
        if (question.Options.Count > 0)
        {
            if (request.SelectedIndex is null || request.SelectedIndex < 0 || request.SelectedIndex >= question.Options.Count || request.Text != null)
                throw new ArgumentException("Choose one valid answer.");
        }
        else if ((string.IsNullOrWhiteSpace(request.Text) && !ExistingAudio(attempt, request.QuestionId)) ||
            request.Text?.Length > 10000 || request.SelectedIndex != null)
            throw new ArgumentException("Enter a response of at most 10,000 characters.");
        var answers = JsonSerializer.Deserialize<Dictionary<Guid, SpecialPracticeAnswer>>(attempt.AnswersJson) ?? [];
        answers[request.QuestionId] = question.Options.Count > 0
            ? new SpecialPracticeAnswer(request.SelectedIndex, null)
            : new SpecialPracticeAnswer(null, request.Text?.Trim(), answers.GetValueOrDefault(request.QuestionId)?.AudioUrl);
        attempt.AnswersJson = JsonSerializer.Serialize(answers);
        attempt.UpdatedAt = DateTime.UtcNow;
        await attempts.SaveAsync(attempt, cancellationToken);
        return Map(attempt);
    }

    public async Task<SpecialPracticeAttemptResponse> SaveAudioAsync(Guid userId, Guid attemptId, Guid questionId,
        string audioUrl, CancellationToken cancellationToken)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, cancellationToken);
        if (attempt.Status != "InProgress" || IsExpired(attempt))
            throw new ArgumentException("This practice attempt is no longer active.");
        using var document = JsonDocument.Parse(attempt.ContentSnapshotJson);
        var question = Questions(document.RootElement, attempt.Kind).FirstOrDefault(item => item.Id == questionId);
        if (question?.Skill != "speaking") throw new ArgumentException("Audio is only accepted for speaking questions.");
        if (!audioUrl.StartsWith("/api/v1/media/", StringComparison.Ordinal))
            throw new ArgumentException("Invalid audio URL.");
        var answers = JsonSerializer.Deserialize<Dictionary<Guid, SpecialPracticeAnswer>>(attempt.AnswersJson) ?? [];
        var existing = answers.GetValueOrDefault(questionId);
        answers[questionId] = new SpecialPracticeAnswer(null, existing?.Text, audioUrl);
        attempt.AnswersJson = JsonSerializer.Serialize(answers);
        attempt.UpdatedAt = DateTime.UtcNow;
        await attempts.SaveAsync(attempt, cancellationToken);
        return Map(attempt);
    }

    private static bool ExistingAudio(SpecialPracticeAttempt attempt, Guid questionId) =>
        (JsonSerializer.Deserialize<Dictionary<Guid, SpecialPracticeAnswer>>(attempt.AnswersJson) ?? [])
            .GetValueOrDefault(questionId)?.AudioUrl is { Length: > 0 };

    public async Task<SpecialPracticeAttemptResponse> SubmitAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, cancellationToken);
        if (attempt.Status != "InProgress") return Map(attempt);
        await FinalizeAsync(attempt, IsExpired(attempt), cancellationToken);
        return Map(attempt);
    }

    private async Task FinalizeAsync(SpecialPracticeAttempt attempt, bool timedOut, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(attempt.ContentSnapshotJson);
        var questions = Questions(document.RootElement, attempt.Kind);
        var answers = JsonSerializer.Deserialize<Dictionary<Guid, SpecialPracticeAnswer>>(attempt.AnswersJson) ?? [];
        if (questions.Count == 0 || (!timedOut && questions.Any(item => !answers.ContainsKey(item.Id))))
            throw new ArgumentException("Answer every question before submitting.");
        var objectiveScore = 0;
        var objectiveMax = 0;
        var subjective = false;
        foreach (var question in questions)
        {
            if (question.Options.Count == 0) { subjective |= answers.ContainsKey(question.Id); continue; }
            objectiveMax += question.Points;
            var source = FindQuestion(document.RootElement, attempt.Kind, question.Id);
            if (source.TryGetProperty("correctIndex", out var correctIndex) &&
                answers.TryGetValue(question.Id, out var answer) && answer.SelectedIndex == correctIndex.GetInt32())
                objectiveScore += question.Points;
        }
        attempt.ObjectiveScore = objectiveScore;
        attempt.ObjectiveMaxScore = objectiveMax;
        attempt.SubjectiveMaxScore = questions.Where(item => item.Options.Count == 0).Sum(item => item.Points);
        attempt.Status = subjective ? "PendingReview" : "Scored";
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.UpdatedAt = attempt.SubmittedAt;
        await attempts.SaveAsync(attempt, cancellationToken);
    }

    private static DateTime? Deadline(SpecialPracticeAttempt attempt)
    {
        using var content = JsonDocument.Parse(attempt.ContentSnapshotJson);
        return content.RootElement.TryGetProperty("durationMinutes", out var duration) &&
            duration.TryGetInt32(out var minutes) && minutes > 0
            ? attempt.CreatedAt.AddMinutes(minutes) : null;
    }

    private static bool IsExpired(SpecialPracticeAttempt attempt) => Deadline(attempt) is { } deadline && DateTime.UtcNow >= deadline;

    public async Task<List<SpecialPracticeReviewItem>> GetPendingReviewsAsync(CancellationToken cancellationToken)
    {
        var pending = await attempts.GetPendingAsync(cancellationToken);
        return pending.Select(item =>
        {
            using var document = JsonDocument.Parse(item.ContentSnapshotJson);
            return new SpecialPracticeReviewItem(item.Id, item.UserId, item.Kind, Read(document.RootElement, "title"),
                item.SubmittedAt ?? item.CreatedAt);
        }).ToList();
    }

    public async Task<SpecialPracticeReviewDetail> GetReviewAsync(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await attempts.GetForReviewAsync(attemptId, cancellationToken)
            ?? throw new NotFoundException("Practice attempt was not found.");
        if (attempt.Status is not ("PendingReview" or "Reviewed"))
            throw new ArgumentException("This attempt is not ready for review.");
        using var document = JsonDocument.Parse(attempt.ContentSnapshotJson);
        var content = document.RootElement;
        return new SpecialPracticeReviewDetail(attempt.Id, attempt.Kind, Read(content, "title"),
            Read(content, "rubric"), Questions(content, attempt.Kind),
            JsonSerializer.Deserialize<Dictionary<Guid, SpecialPracticeAnswer>>(attempt.AnswersJson) ?? [],
            attempt.ObjectiveScore, attempt.ObjectiveMaxScore);
    }

    public async Task<SpecialPracticeAttemptResponse> GradeAsync(Guid attemptId, GradeSpecialPracticeRequest request,
        CancellationToken cancellationToken)
    {
        var attempt = await attempts.GetForReviewAsync(attemptId, cancellationToken)
            ?? throw new NotFoundException("Practice attempt was not found.");
        if (attempt.Status != "PendingReview") throw new ArgumentException("This attempt is not pending review.");
        using var document = JsonDocument.Parse(attempt.ContentSnapshotJson);
        var subjective = Questions(document.RootElement, attempt.Kind).Where(item => item.Options.Count == 0).ToList();
        if (string.IsNullOrWhiteSpace(request.Feedback) || request.Feedback.Length > 5000 ||
            request.Scores.Count != subjective.Count || subjective.Any(item =>
                !request.Scores.TryGetValue(item.Id, out var score) || score < 0 || score > item.Points))
            throw new ArgumentException("Score every subjective response within its point range and provide feedback.");
        attempt.SubjectiveScore = request.Scores.Values.Sum();
        attempt.SubjectiveMaxScore = subjective.Sum(item => item.Points);
        attempt.EvaluationJson = JsonSerializer.Serialize(new { request.Scores, Feedback = request.Feedback.Trim() });
        attempt.Status = "Reviewed";
        attempt.ReviewedAt = DateTime.UtcNow;
        attempt.UpdatedAt = attempt.ReviewedAt;
        await attempts.SaveAsync(attempt, cancellationToken);
        return Map(attempt);
    }

    public async Task UpdateContentAsync(Guid courseId, JsonElement content, CancellationToken cancellationToken)
    {
        var course = await courses.GetCourseByIdAsync(courseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Course was not found.");
        ValidateContent(content, course.Level, course.Language.Code);
        var material = string.IsNullOrWhiteSpace(course.MaterialJson)
            ? new JsonObject { ["id"] = course.Id.ToString(), ["languageCode"] = course.Language.Code,
                ["level"] = course.Level, ["domain"] = course.Domain, ["title"] = course.Title,
                ["description"] = course.Description, ["units"] = new JsonArray(), ["decks"] = new JsonArray(),
                ["placementTests"] = new JsonArray(), ["srs"] = new JsonObject { ["algorithm"] = "srs-v1", ["dailyGoal"] = 10 } }
            : JsonNode.Parse(course.MaterialJson) as JsonObject ?? throw new ArgumentException("Invalid course material.");
        material["certificateExams"] = JsonNode.Parse(content.GetProperty("certificateExams").GetRawText());
        material["interviewScenarios"] = JsonNode.Parse(content.GetProperty("interviewScenarios").GetRawText());
        course.MaterialJson = material.ToJsonString();
        await courses.UpdateCourseAsync(course, cancellationToken);
    }

    public async Task<JsonElement> GetAdminContentAsync(Guid courseId, string kind, CancellationToken cancellationToken)
    {
        ValidateKind(kind);
        var course = await courses.GetCourseByIdAsync(courseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Course was not found.");
        using var material = JsonDocument.Parse(course.MaterialJson ?? "{}");
        return JsonSerializer.SerializeToElement(GetModules(material.RootElement, kind).ToArray());
    }

    public Task<JsonElement> CreateAdminContentAsync(Guid courseId, string kind, JsonElement item, CancellationToken cancellationToken) =>
        MutateAdminContentAsync(courseId, kind, null, item, false, cancellationToken);

    public Task<JsonElement> UpdateAdminContentAsync(Guid courseId, string kind, Guid itemId, JsonElement item, CancellationToken cancellationToken) =>
        MutateAdminContentAsync(courseId, kind, itemId, item, false, cancellationToken);

    public async Task DeleteAdminContentAsync(Guid courseId, string kind, Guid itemId, CancellationToken cancellationToken) =>
        await MutateAdminContentAsync(courseId, kind, itemId, default, true, cancellationToken);

    public async Task<JsonElement> ImportAdminContentAsync(Guid courseId, string kind, JsonElement items, CancellationToken cancellationToken)
    {
        ValidateKind(kind);
        if (items.ValueKind != JsonValueKind.Array) throw new ArgumentException("Import content must be an array.");
        var course = await courses.GetCourseByIdAsync(courseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Course was not found.");
        var material = JsonNode.Parse(course.MaterialJson ?? "{}") as JsonObject
            ?? throw new ArgumentException("Invalid course material.");
        var key = kind == "certificate" ? "certificateExams" : "interviewScenarios";
        var list = material[key] as JsonArray ?? new JsonArray();
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) throw new ArgumentException("Practice item must be an object.");
            var id = ParseId(item);
            var index = Enumerable.Range(0, list.Count)
                .FirstOrDefault(i => Guid.TryParse(list[i]?["id"]?.ToString(), out var existing) && existing == id, -1);
            var node = JsonNode.Parse(item.GetRawText());
            if (index < 0) list.Add(node); else list[index] = node;
        }
        if (material[key] is null) material[key] = list;
        material["certificateExams"] ??= new JsonArray();
        material["interviewScenarios"] ??= new JsonArray();
        using var validation = JsonDocument.Parse(material.ToJsonString());
        ValidateContent(validation.RootElement, course.Level, course.Language.Code);
        course.MaterialJson = material.ToJsonString();
        await courses.UpdateCourseAsync(course, cancellationToken);
        return JsonSerializer.SerializeToElement(list);
    }

    private async Task<JsonElement> MutateAdminContentAsync(Guid courseId, string kind, Guid? itemId,
        JsonElement item, bool delete, CancellationToken cancellationToken)
    {
        ValidateKind(kind);
        var course = await courses.GetCourseByIdAsync(courseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Course was not found.");
        var material = JsonNode.Parse(course.MaterialJson ?? "{}") as JsonObject
            ?? throw new ArgumentException("Invalid course material.");
        var key = kind == "certificate" ? "certificateExams" : "interviewScenarios";
        var list = material[key] as JsonArray ?? new JsonArray();
        var index = itemId is null ? -1 : Enumerable.Range(0, list.Count)
            .FirstOrDefault(i => Guid.TryParse(list[i]?["id"]?.ToString(), out var id) && id == itemId, -1);
        if (itemId is not null && index < 0) throw new NotFoundException("Practice content was not found.");
        if (delete) list.RemoveAt(index);
        else
        {
            if (item.ValueKind != JsonValueKind.Object) throw new ArgumentException("Practice item must be an object.");
            var parsedId = ParseId(item);
            if (itemId is not null && parsedId != itemId)
                throw new ArgumentException("The item ID cannot change.");
            var node = JsonNode.Parse(item.GetRawText());
            if (index < 0) list.Add(node); else list[index] = node;
        }
        if (material[key] is null) material[key] = list;
        material["certificateExams"] ??= new JsonArray();
        material["interviewScenarios"] ??= new JsonArray();
        using var validation = JsonDocument.Parse(material.ToJsonString());
        ValidateContent(validation.RootElement, course.Level, course.Language.Code);
        course.MaterialJson = material.ToJsonString();
        await courses.UpdateCourseAsync(course, cancellationToken);
        return delete ? JsonSerializer.SerializeToElement(new { deleted = true }) : item.Clone();
    }

    public void ValidateContent(JsonElement content, string level, string languageCode)
    {
        if (content.ValueKind != JsonValueKind.Object ||
            !content.TryGetProperty("certificateExams", out var exams) || exams.ValueKind != JsonValueKind.Array ||
            !content.TryGetProperty("interviewScenarios", out var interviews) || interviews.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("certificateExams and interviewScenarios must be arrays.");
        if (!string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase) && exams.GetArrayLength() > 0)
            throw new ArgumentException("IELTS and TOEIC content requires an English course.");
        var ids = new HashSet<Guid>();
        foreach (var exam in exams.EnumerateArray())
        {
            ValidateIdentity(exam, level, ids);
            if (exam.TryGetProperty("durationMinutes", out var duration) &&
                (!duration.TryGetInt32(out var minutes) || minutes is < 1 or > 240))
                throw new ArgumentException("Exam durationMinutes must be between 1 and 240.");
            if (Read(exam, "certificate") is not ("IELTS" or "TOEIC"))
                throw new ArgumentException("certificate must be IELTS or TOEIC.");
            if (!exam.TryGetProperty("sections", out var sections) || sections.ValueKind != JsonValueKind.Array || sections.GetArrayLength() == 0)
                throw new ArgumentException("A certificate exam needs at least one section.");
            foreach (var section in sections.EnumerateArray())
            {
                var skill = Read(section, "skill").ToLowerInvariant();
                if (skill is not ("listening" or "reading" or "speaking" or "writing") ||
                    !section.TryGetProperty("questions", out var questions) || questions.ValueKind != JsonValueKind.Array || questions.GetArrayLength() == 0)
                    throw new ArgumentException("Each exam section needs a valid skill and questions.");
                foreach (var question in questions.EnumerateArray()) ValidateQuestion(question, skill, ids);
            }
        }
        foreach (var interview in interviews.EnumerateArray())
        {
            ValidateIdentity(interview, level, ids);
            Required(interview, "profession"); Required(interview, "rubric");
            if (!interview.TryGetProperty("turns", out var turns) || turns.ValueKind != JsonValueKind.Array || turns.GetArrayLength() == 0)
                throw new ArgumentException("An interview needs at least one turn.");
            foreach (var turn in turns.EnumerateArray())
            {
                if (!ids.Add(ParseId(turn))) throw new ArgumentException("Practice IDs must be unique.");
                Required(turn, "prompt");
                if (turn.TryGetProperty("points", out var points) && (!points.TryGetInt32(out var score) || score is < 1 or > 100))
                    throw new ArgumentException("Interview turn points must be between 1 and 100.");
            }
        }
    }

    private async Task<Course> GetAccessibleCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin() && !await access.CanAccessCourseAsync(userId, courseId, cancellationToken))
            throw new NotFoundException("Course was not found.");
        var course = await courses.GetCourseByIdAsync(courseId, cancellationToken: cancellationToken);
        if (course?.Status != ContentStatus.Published) throw new NotFoundException("Course was not found.");
        return course;
    }

    private async Task<SpecialPracticeAttempt> GetOwnedAttemptAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await attempts.GetAsync(userId, attemptId, cancellationToken)
            ?? throw new NotFoundException("Practice attempt was not found.");
        await GetAccessibleCourseAsync(userId, attempt.CourseId, cancellationToken);
        return attempt;
    }

    private static SpecialPracticeAttemptResponse Map(SpecialPracticeAttempt attempt)
    {
        using var document = JsonDocument.Parse(attempt.ContentSnapshotJson);
        var content = document.RootElement;
        return new SpecialPracticeAttemptResponse(attempt.Id, attempt.CourseId, attempt.ContentId,
            attempt.Kind, Read(content, "title"), Read(content, "certificate"), Read(content, "profession"),
            attempt.Status, Questions(content, attempt.Kind),
            JsonSerializer.Deserialize<Dictionary<Guid, SpecialPracticeAnswer>>(attempt.AnswersJson) ?? [],
            attempt.ObjectiveScore, attempt.ObjectiveMaxScore, attempt.SubjectiveScore,
            attempt.SubjectiveMaxScore, ReadFeedback(attempt.EvaluationJson), attempt.SubmittedAt, attempt.ReviewedAt, Deadline(attempt));
    }

    private static List<SpecialPracticeQuestion> Questions(JsonElement content, string kind)
    {
        var questions = new List<SpecialPracticeQuestion>();
        if (kind == "interview")
        {
            if (!content.TryGetProperty("turns", out var turns) || turns.ValueKind != JsonValueKind.Array) return questions;
            foreach (var turn in turns.EnumerateArray())
                if (Guid.TryParse(Read(turn, "id"), out var id))
                    questions.Add(new SpecialPracticeQuestion(id, "speaking", Read(turn, "prompt"),
                        Read(turn, "context"), "", "", [], Read(content, "rubric"),
                        turn.TryGetProperty("points", out var points) && points.TryGetInt32(out var score) ? score : 10));
            return questions;
        }
        if (!content.TryGetProperty("sections", out var sections) || sections.ValueKind != JsonValueKind.Array) return questions;
        foreach (var section in sections.EnumerateArray())
        {
            var skill = Read(section, "skill");
            if (!section.TryGetProperty("questions", out var items) || items.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in items.EnumerateArray())
                if (Guid.TryParse(Read(item, "id"), out var id))
                    questions.Add(new SpecialPracticeQuestion(id, skill, Read(item, "prompt"),
                        Read(item, "passage"), Read(item, "audioScript"), Read(item, "audioUrl"),
                        item.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array
                            ? options.EnumerateArray().Select(value => value.GetString() ?? "").ToList() : [],
                        Read(item, "rubric"),
                        item.TryGetProperty("points", out var points) && points.TryGetInt32(out var score) ? score : 0));
        }
        return questions;
    }

    private static JsonElement FindQuestion(JsonElement content, string kind, Guid questionId)
    {
        if (kind == "interview") return content.GetProperty("turns").EnumerateArray()
            .First(item => Guid.TryParse(Read(item, "id"), out var id) && id == questionId);
        return content.GetProperty("sections").EnumerateArray()
            .SelectMany(section => section.GetProperty("questions").EnumerateArray())
            .First(item => Guid.TryParse(Read(item, "id"), out var id) && id == questionId);
    }

    private static IEnumerable<JsonElement> GetModules(JsonElement material, string kind) =>
        material.TryGetProperty(kind == "certificate" ? "certificateExams" : "interviewScenarios", out var items) &&
        items.ValueKind == JsonValueKind.Array ? items.EnumerateArray() : [];

    private static void ValidateKind(string kind)
    {
        if (kind is not ("certificate" or "interview")) throw new ArgumentException("Unknown practice kind.");
    }

    private static void ValidateIdentity(JsonElement item, string level, HashSet<Guid> ids)
    {
        if (!ids.Add(ParseId(item))) throw new ArgumentException("Practice IDs must be unique.");
        Required(item, "title");
        if (!string.Equals(Read(item, "level"), level, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Practice level must match the course level.");
    }

    private static void ValidateQuestion(JsonElement question, string skill, HashSet<Guid> ids)
    {
        if (!ids.Add(ParseId(question))) throw new ArgumentException("Practice IDs must be unique.");
        Required(question, "prompt");
        if (!question.TryGetProperty("points", out var points) || !points.TryGetInt32(out var value) || value is < 1 or > 100)
            throw new ArgumentException("Question points must be between 1 and 100.");
        if (skill is "listening" or "reading")
        {
            if (!question.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array ||
                options.GetArrayLength() is < 2 or > 8 ||
                options.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString())) ||
                !question.TryGetProperty("correctIndex", out var answer) || !answer.TryGetInt32(out var index) ||
                index < 0 || index >= options.GetArrayLength())
                throw new ArgumentException("Listening and reading need valid options and correctIndex.");
            if (skill == "listening" && string.IsNullOrWhiteSpace(Read(question, "audioScript")) &&
                string.IsNullOrWhiteSpace(Read(question, "audioUrl")))
                throw new ArgumentException("Listening needs audioScript or audioUrl.");
            if (skill == "reading") Required(question, "passage");
        }
        else Required(question, "rubric");
    }

    private static Guid ParseId(JsonElement item) => Guid.TryParse(Read(item, "id"), out var id) && id != Guid.Empty
        ? id : throw new ArgumentException("Practice IDs must be non-empty UUIDs.");
    private static void Required(JsonElement item, string property)
    {
        if (string.IsNullOrWhiteSpace(Read(item, property))) throw new ArgumentException($"{property} is required.");
    }
    private static string Read(JsonElement item, string property) =>
        item.ValueKind == JsonValueKind.Object && item.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static string ReadFeedback(string evaluationJson)
    {
        using var document = JsonDocument.Parse(evaluationJson);
        return Read(document.RootElement, "Feedback");
    }

    private static bool MatchesProfession(LanguageLearning.Application.DTOs.Auth.StudyTargetDto? target, string profession) =>
        target != null && (target.Domains?.Count > 0 ? target.Domains : [target.Domain])
            .Any(domain => string.Equals(domain, profession, StringComparison.OrdinalIgnoreCase));
}
