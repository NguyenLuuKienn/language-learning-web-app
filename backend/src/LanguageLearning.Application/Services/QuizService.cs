using System.Text.Json;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Quiz;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public class QuizService : IQuizService
{
    private readonly IQuizRepository _quizRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILessonJourneyService _journey;

    public QuizService(
        IQuizRepository quizRepository,
        IProgressRepository progressRepository,
        IUserRepository userRepository, ILessonJourneyService journey)
    {
        _quizRepository = quizRepository;
        _progressRepository = progressRepository;
        _userRepository = userRepository;
        _journey = journey;
    }

    public async Task<List<QuizQuestionResponse>> GetLessonQuizAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var questions = await _quizRepository.GetQuestionsByLessonIdAsync(lessonId, cancellationToken);

        return questions.Select(q =>
        {
            var matchingPairs = q.Type == QuestionType.Matching ? GetMatchingPairs(q.ConfigJson) : new Dictionary<string, string>();
            return new QuizQuestionResponse
        {
            Id = q.Id,
            LessonId = q.LessonId,
            Type = q.Type,
            Prompt = q.Prompt,
            ConfigJson = "{}",
            MatchingTerms = matchingPairs.Keys.ToList(),
            MatchingChoices = matchingPairs.Values.OrderBy(_ => Guid.NewGuid()).ToList(),
            Options = q.Options
                .OrderBy(o => o.SortOrder)
                .Select(o => new QuestionOptionResponse
                {
                    Id = o.Id,
                    Content = o.Content,
                    SortOrder = o.SortOrder
                }).ToList()
        };
        }).ToList();
    }

    private static Dictionary<string, string> GetMatchingPairs(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return new();
        try
        {
            using var document = JsonDocument.Parse(configJson);
            if (!document.RootElement.TryGetProperty("pairs", out var pairs) || pairs.ValueKind != JsonValueKind.Object) return new();
            return pairs.EnumerateObject().ToDictionary(
                pair => pair.Name,
                pair => pair.Value.ValueKind == JsonValueKind.String ? pair.Value.GetString() ?? string.Empty : string.Empty);
        }
        catch (JsonException)
        {
            return new();
        }
    }

    public async Task<QuizResultResponse> SubmitQuizAsync(Guid userId, SubmitQuizRequest request, CancellationToken cancellationToken = default)
    {
        var questions = await _quizRepository.GetQuestionsByLessonIdAsync(request.LessonId, cancellationToken);
        if (!questions.Any())
        {
            throw new NotFoundException(Messages.Quiz.QuestionNotFound);
        }
        if (request.ClientAttemptId is { } clientAttemptId)
        {
            var existing = await _quizRepository.GetAttemptByClientIdAsync(userId, clientAttemptId, cancellationToken);
            if (existing != null)
            {
                if (existing.LessonId != request.LessonId)
                    throw new BusinessRuleException("QUIZ_ATTEMPT_CONFLICT", "This quiz attempt identifier belongs to another lesson.");
                await _journey.RefreshAsync(userId, request.LessonId, cancellationToken);
                return MapExisting(existing, questions);
            }
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var timezone = user?.Timezone ?? "Asia/Ho_Chi_Minh";

        var submissionMap = request.Answers.ToDictionary(a => a.QuestionId);
        int score = 0;
        int maxScore = questions.Count;

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LessonId = request.LessonId,
            ClientAttemptId = request.ClientAttemptId,
            MaxScore = maxScore,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var feedbacks = new List<QuestionFeedbackResponse>();

        foreach (var q in questions)
        {
            bool isCorrect = false;
            string recordedAnswerJson = "{}";

            if (submissionMap.TryGetValue(q.Id, out var sub))
            {
                if (q.Type == QuestionType.MultipleChoice)
                {
                    var correctOpt = q.Options.FirstOrDefault(o => o.IsCorrect);
                    isCorrect = sub.SelectedOptionId.HasValue && correctOpt != null && sub.SelectedOptionId.Value == correctOpt.Id;
                    recordedAnswerJson = JsonSerializer.Serialize(new { selectedOptionId = sub.SelectedOptionId });
                }
                else if (q.Type == QuestionType.FillInBlank)
                {
                    var validAnswers = ExtractValidAnswers(q);
                    var userAns = sub.TextAnswer?.Trim();
                    isCorrect = !string.IsNullOrWhiteSpace(userAns) && validAnswers.Any(a => string.Equals(a, userAns, StringComparison.OrdinalIgnoreCase));
                    recordedAnswerJson = JsonSerializer.Serialize(new { textAnswer = sub.TextAnswer });
                }
                else if (q.Type == QuestionType.Matching)
                {
                    isCorrect = EvaluateMatchingAnswer(q, sub.MatchingJson);
                    recordedAnswerJson = sub.MatchingJson ?? "{}";
                }
            }

            if (isCorrect)
            {
                score++;
            }

            attempt.Answers.Add(new QuizAnswer
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                QuestionId = q.Id,
                AnswerJson = recordedAnswerJson,
                IsCorrect = isCorrect,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            feedbacks.Add(new QuestionFeedbackResponse
            {
                QuestionId = q.Id,
                IsCorrect = isCorrect,
                Explanation = q.Explanation
            });
        }

        attempt.Score = score;
        if (!await _quizRepository.AddAttemptAsync(attempt, cancellationToken))
        {
            var existing = await _quizRepository.GetAttemptByClientIdAsync(userId, request.ClientAttemptId!.Value, cancellationToken)
                ?? throw new BusinessRuleException("QUIZ_ATTEMPT_CONFLICT", "The quiz attempt could not be recovered.");
            if (existing.LessonId != request.LessonId)
                throw new BusinessRuleException("QUIZ_ATTEMPT_CONFLICT", "This quiz attempt identifier belongs to another lesson.");
            await _journey.RefreshAsync(userId, request.LessonId, cancellationToken);
            return MapExisting(existing, questions);
        }

        double percentage = maxScore > 0 ? (double)score / maxScore * 100 : 0;
        await _journey.RefreshAsync(userId, request.LessonId, cancellationToken);

        return new QuizResultResponse
        {
            AttemptId = attempt.Id,
            LessonId = request.LessonId,
            Score = score,
            MaxScore = maxScore,
            CompletedAt = attempt.CompletedAt,
            Details = feedbacks
        };
    }

    private static QuizResultResponse MapExisting(QuizAttempt attempt, IReadOnlyCollection<Question> questions) => new()
    {
        AttemptId = attempt.Id, LessonId = attempt.LessonId,
        Score = attempt.Score, MaxScore = attempt.MaxScore, CompletedAt = attempt.CompletedAt,
        Details = questions.Select(question => new QuestionFeedbackResponse
        {
            QuestionId = question.Id,
            IsCorrect = attempt.Answers.Any(answer => answer.QuestionId == question.Id && answer.IsCorrect),
            Explanation = question.Explanation
        }).ToList()
    };

    public async Task<List<QuizAttemptHistoryResponse>> GetAttemptHistoryAsync(Guid userId, Guid? lessonId = null, CancellationToken cancellationToken = default)
    {
        var attempts = await _quizRepository.GetUserAttemptsAsync(userId, lessonId, 50, cancellationToken);

        return attempts.Select(a => new QuizAttemptHistoryResponse
        {
            Id = a.Id,
            LessonId = a.LessonId,
            LessonTitle = a.Lesson?.Title ?? string.Empty,
            Score = a.Score,
            MaxScore = a.MaxScore,
            CompletedAt = a.CompletedAt
        }).ToList();
    }

    private static List<string> ExtractValidAnswers(Question q)
    {
        var answers = new List<string>();
        foreach (var opt in q.Options.Where(o => o.IsCorrect))
        {
            answers.Add(opt.Content.Trim());
        }

        if (!string.IsNullOrWhiteSpace(q.ConfigJson) && q.ConfigJson != "{}")
        {
            try
            {
                using var doc = JsonDocument.Parse(q.ConfigJson);
                if (doc.RootElement.TryGetProperty("answers", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in arr.EnumerateArray())
                    {
                        var str = elem.GetString();
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            answers.Add(str.Trim());
                        }
                    }
                }
            }
            catch
            {
            }
        }

        return answers;
    }

    private static bool EvaluateMatchingAnswer(Question q, string? matchingJson)
    {
        if (string.IsNullOrWhiteSpace(matchingJson) || string.IsNullOrWhiteSpace(q.ConfigJson))
        {
            return false;
        }

        try
        {
            using var userDoc = JsonDocument.Parse(matchingJson);
            using var correctDoc = JsonDocument.Parse(q.ConfigJson);

            if (!correctDoc.RootElement.TryGetProperty("pairs", out var correctPairs))
            {
                return false;
            }

            if (!userDoc.RootElement.TryGetProperty("pairs", out var userPairs))
            {
                return false;
            }

            var correctDict = new Dictionary<string, string>();
            foreach (var prop in correctPairs.EnumerateObject())
            {
                correctDict[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }

            var userDict = new Dictionary<string, string>();
            foreach (var prop in userPairs.EnumerateObject())
            {
                userDict[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }

            if (correctDict.Count != userDict.Count)
            {
                return false;
            }

            foreach (var kvp in correctDict)
            {
                if (!userDict.TryGetValue(kvp.Key, out var userVal) || !string.Equals(userVal, kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
