using LanguageLearning.Application.Common;
using System.Text.Json;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public sealed class PlacementService : IPlacementService
{
    private static readonly string[] RequiredSections = ["listening", "speaking", "reading", "writing"];
    private readonly ICourseRepository _courses;
    private readonly IPlacementProofService _proofs;
    private readonly IPlacementReviewRepository _reviews;
    private readonly IAdminRepository _admin;

    public PlacementService(ICourseRepository courses, IPlacementProofService proofs, IPlacementReviewRepository reviews, IAdminRepository admin)
    {
        _courses = courses;
        _proofs = proofs;
        _reviews = reviews;
        _admin = admin;
    }

    public async Task<PlacementExamDto> GetExamAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var (course, questions) = await LoadQuestionsAsync(courseId, cancellationToken);
        return new PlacementExamDto
        {
            CourseId = courseId,
            LanguageCode = course.Language.Code,
            Level = course.Level,
            Questions = questions.Select(item => new PlacementQuestionDto
            {
                Id = item.Id,
                Section = item.Section,
                Prompt = item.Data.GetProperty("prompt").GetString() ?? string.Empty,
                AudioScript = ReadOptional(item.Data, "audioScript"),
                ReadingPassage = ReadOptional(item.Data, "readingPassage"),
                Options = item.Data.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array
                    ? options.EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToList() : null,
                MinWords = item.Data.TryGetProperty("minWords", out var minWords) && minWords.ValueKind == JsonValueKind.Number
                    ? minWords.GetInt32() : null,
                Points = item.Points
            }).ToList()
        };
    }

    public async Task<PlacementResultDto> SubmitAsync(Guid userId, Guid courseId, PlacementSubmissionDto submission, CancellationToken cancellationToken = default)
    {
        var (_, questions) = await LoadQuestionsAsync(courseId, cancellationToken);
        if (submission.SelectedOptions == null || submission.WrittenAnswers == null
            || submission.AudioFileNames == null
            || submission.SelectedOptions.Count > questions.Count || submission.WrittenAnswers.Count > questions.Count
            || submission.AudioFileNames.Count > questions.Count
            || submission.WrittenAnswers.Values.Any(answer => answer == null || answer.Length > 4000))
            throw new ArgumentException(Messages.Placement.SubmissionTooLarge);
        foreach (var question in questions.Where(question => question.Section == "speaking"))
        {
            if (!submission.AudioFileNames.TryGetValue(question.Id, out var fileName)
                || !await _reviews.OwnsAudioAsync(userId, courseId, question.Id, fileName, cancellationToken))
                throw new ArgumentException(Messages.Placement.SpeakingAudioRequired);
        }
        if (questions.Where(question => question.Section == "writing")
            .Any(question => !submission.WrittenAnswers.TryGetValue(question.Id, out var answer) || string.IsNullOrWhiteSpace(answer)))
            throw new ArgumentException(Messages.Placement.WritingAnswerRequired);
        var attempt = new PlacementAttempt
        {
            Id = Guid.NewGuid(), UserId = userId, CourseId = courseId,
            QuestionsJson = JsonSerializer.Serialize(questions),
            AnswersJson = JsonSerializer.Serialize(submission),
            AudioFilesJson = JsonSerializer.Serialize(submission.AudioFileNames)
        };
        await _reviews.AddAttemptAsync(attempt, cancellationToken);
        return await BuildResultAsync(attempt, cancellationToken);
    }

    public async Task RegisterAudioAsync(Guid userId, Guid courseId, string questionId, string fileName, CancellationToken cancellationToken = default)
    {
        var exam = await GetExamAsync(courseId, cancellationToken);
        if (!exam.Questions.Any(question => question.Id == questionId && question.Section == "speaking"))
            throw new ArgumentException(Messages.Placement.AudioQuestionMismatch);
        await _reviews.AddAudioAsync(new PlacementAudio
        {
            Id = Guid.NewGuid(), UserId = userId, CourseId = courseId,
            QuestionId = questionId, FileName = fileName
        }, cancellationToken);
    }

    public async Task<PlacementResultDto> GetResultAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _reviews.GetAttemptAsync(attemptId, cancellationToken);
        if (attempt == null || attempt.UserId != userId) throw new NotFoundException(Messages.Placement.AttemptNotFound);
        return await BuildResultAsync(attempt, cancellationToken);
    }

    public async Task<PlacementResultDto?> GetLatestResultAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default)
    {
        var attempt = await _reviews.GetLatestAttemptAsync(userId, courseId, cancellationToken);
        return attempt == null ? null : await BuildResultAsync(attempt, cancellationToken);
    }

    public async Task<List<PlacementReviewDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var attempts = await _reviews.GetPendingAsync(cancellationToken);
        if (attempts.Count == 0) return [];
        var labels = await _reviews.GetPendingLabelsAsync(attempts.Select(attempt => attempt.UserId).Distinct().ToArray(),
            attempts.Select(attempt => attempt.CourseId).Distinct().ToArray(), cancellationToken);
        return attempts.Select(attempt =>
        {
            var questions = JsonSerializer.Deserialize<List<ExamQuestion>>(attempt.QuestionsJson) ?? [];
            var answers = JsonSerializer.Deserialize<PlacementSubmissionDto>(attempt.AnswersJson) ?? new();
            var audios = JsonSerializer.Deserialize<Dictionary<string, string>>(attempt.AudioFilesJson) ?? [];
            return new PlacementReviewDto
            {
                AttemptId = attempt.Id, CourseId = attempt.CourseId, UserId = attempt.UserId,
                LearnerName = labels.Learners.GetValueOrDefault(attempt.UserId, attempt.UserId.ToString()),
                CourseTitle = labels.Courses.GetValueOrDefault(attempt.CourseId, attempt.CourseId.ToString()),
                SubmittedAt = attempt.CreatedAt,
                Questions = questions.Select(question => new PlacementReviewQuestionDto
                {
                    Id = question.Id, Section = question.Section, Points = question.Points,
                    Prompt = question.Data.GetProperty("prompt").GetString() ?? string.Empty,
                    Rubric = ReadOptional(question.Data, "rubric"),
                    ExpectedResponse = ReadOptional(question.Data, "expectedResponse"),
                    WrittenAnswer = answers.WrittenAnswers.GetValueOrDefault(question.Id),
                    AudioUrl = audios.TryGetValue(question.Id, out var fileName) ? "/api/v1/media/" + fileName : null
                }).ToList()
            };
        }).ToList();
    }

    public async Task<PlacementResultDto> ReviewAsync(Guid adminId, Guid attemptId, PlacementReviewRequestDto review, CancellationToken cancellationToken = default)
    {
        var attempt = await _reviews.GetAttemptAsync(attemptId, cancellationToken)
            ?? throw new NotFoundException(Messages.Placement.AttemptNotFound);
        if (attempt.Status != "Pending") throw new ArgumentException(Messages.Placement.AlreadyReviewed);
        var questions = JsonSerializer.Deserialize<List<ExamQuestion>>(attempt.QuestionsJson) ?? [];
        var subjective = questions.Where(question => question.Section is "speaking" or "writing").ToList();
        if (review.Scores == null || review.Scores.Count != subjective.Count || review.Feedback == null || review.Feedback.Length > 4000 ||
            subjective.Any(question => !review.Scores.TryGetValue(question.Id, out var score) || score < 0 || score > question.Points))
            throw new ArgumentException(Messages.Placement.ScoresInvalid);
        attempt.ScoresJson = JsonSerializer.Serialize(review.Scores);
        attempt.Feedback = review.Feedback.Trim();
        attempt.ReviewedBy = adminId;
        attempt.ReviewedAt = DateTime.UtcNow;
        var breakdown = CalculateBreakdown(attempt);
        var total = breakdown.Values.Sum(section => section.Total);
        var earned = breakdown.Values.Sum(section => section.Earned);
        attempt.Status = total > 0 && earned * 100 >= total * 75 &&
            breakdown.Values.All(section => section.Total > 0 && section.Earned * 2 >= section.Total)
            ? "Passed" : "Failed";
        await _reviews.SaveAttemptAsync(cancellationToken);
        await _admin.RecordAuditLogAsync(new AuditLog
        {
            Id = Guid.NewGuid(), ActorId = adminId, Action = "PlacementReviewed",
            TargetType = "PlacementAttempt", TargetId = attemptId,
            MetadataJson = JsonSerializer.Serialize(new { attempt.CourseId, attempt.UserId, attempt.Status })
        }, cancellationToken);
        return await BuildResultAsync(attempt, cancellationToken, includeProof: false);
    }

    private async Task<PlacementResultDto> BuildResultAsync(PlacementAttempt attempt, CancellationToken cancellationToken, bool includeProof = true)
    {
        var course = await _courses.GetCourseByIdAsync(attempt.CourseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        var breakdown = CalculateBreakdown(attempt);
        var total = breakdown.Values.Sum(section => section.Total);
        var earned = breakdown.Values.Sum(section => section.Earned);
        return new PlacementResultDto
        {
            AttemptId = attempt.Id, Status = attempt.Status, Feedback = attempt.Feedback,
            CourseId = attempt.CourseId, LanguageCode = course.Language.Code, TargetLevel = course.Level,
            TotalPoints = total, EarnedPoints = earned,
            Percentage = total == 0 ? 0 : (int)Math.Round(100.0 * earned / total),
            Passed = attempt.Status == "Passed", SectionBreakdown = breakdown,
            AttemptedAt = attempt.CreatedAt,
            PlacementProof = includeProof && attempt.Status == "Passed" ? _proofs.Create(attempt.UserId, attempt.CourseId) : null
        };
    }

    private static Dictionary<string, PlacementSectionScoreDto> CalculateBreakdown(PlacementAttempt attempt)
    {
        var questions = JsonSerializer.Deserialize<List<ExamQuestion>>(attempt.QuestionsJson) ?? [];
        var answers = JsonSerializer.Deserialize<PlacementSubmissionDto>(attempt.AnswersJson) ?? new();
        var scores = JsonSerializer.Deserialize<Dictionary<string, int>>(attempt.ScoresJson) ?? [];
        var breakdown = RequiredSections.ToDictionary(section => section, _ => new PlacementSectionScoreDto());
        foreach (var question in questions)
        {
            var score = question.Section is "speaking" or "writing"
                ? scores.GetValueOrDefault(question.Id)
                : answers.SelectedOptions.TryGetValue(question.Id, out var selected) &&
                  selected == question.Data.GetProperty("correctIndex").GetInt32() ? question.Points : 0;
            breakdown[question.Section].Total += question.Points;
            breakdown[question.Section].Earned += score;
        }
        return breakdown;
    }

    private async Task<(Course Course, List<ExamQuestion> Questions)> LoadQuestionsAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var course = await _courses.GetCourseByIdAsync(courseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        if (course.Status != ContentStatus.Published || string.IsNullOrWhiteSpace(course.MaterialJson))
            throw new NotFoundException(Messages.Placement.ExamUnavailable);
        using var document = JsonDocument.Parse(course.MaterialJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("placementTests", out var tests) || tests.ValueKind != JsonValueKind.Array)
            throw new ArgumentException(Messages.Placement.MaterialInvalid);

        var questions = new List<ExamQuestion>();
        var testIndex = 0;
        foreach (var test in tests.EnumerateArray())
        {
            if (test.TryGetProperty("level", out var level) &&
                string.Equals(level.GetString(), course.Level, StringComparison.OrdinalIgnoreCase) &&
                test.TryGetProperty("questions", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                var questionIndex = 0;
                foreach (var item in items.EnumerateArray())
                {
                    var section = item.GetProperty("section").GetString() ?? string.Empty;
                    if (!RequiredSections.Contains(section)) continue;
                    var points = item.GetProperty("points").GetInt32();
                    if (points <= 0) throw new ArgumentException(Messages.Placement.PointsInvalid);
                    questions.Add(new ExamQuestion($"{courseId}-{testIndex}-{questionIndex}", section, points, item.Clone()));
                    questionIndex++;
                }
                break;
            }
            testIndex++;
        }
        if (!RequiredSections.All(section => questions.Any(question => question.Section == section)))
            throw new ArgumentException(Messages.Placement.FourSkillsRequired);
        return (course, questions);
    }

    private static string? ReadOptional(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private sealed record ExamQuestion(string Id, string Section, int Points, JsonElement Data);
}
