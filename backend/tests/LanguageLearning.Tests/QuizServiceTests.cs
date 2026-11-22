using LanguageLearning.Application.DTOs.Quiz;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class QuizServiceTests
{
    private readonly Mock<IQuizRepository> _quizRepositoryMock;
    private readonly Mock<IProgressRepository> _progressRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILessonJourneyService> _journeyMock;
    private readonly QuizService _quizService;

    public QuizServiceTests()
    {
        _quizRepositoryMock = new Mock<IQuizRepository>();
        _progressRepositoryMock = new Mock<IProgressRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _journeyMock = new Mock<ILessonJourneyService>();
        _quizRepositoryMock.Setup(repository => repository.AddAttemptAsync(It.IsAny<QuizAttempt>(), default)).ReturnsAsync(true);

        _quizService = new QuizService(
            _quizRepositoryMock.Object,
            _progressRepositoryMock.Object,
            _userRepositoryMock.Object,
            _journeyMock.Object);
    }

    [Fact]
    public async Task RetriedQuizWithSameClientAttemptId_ReturnsOriginalResultWithoutAnotherAttempt()
    {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var clientAttemptId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(), UserId = userId, LessonId = lessonId,
            ClientAttemptId = clientAttemptId, Score = 1, MaxScore = 1,
            Answers = [new QuizAnswer { Id = Guid.NewGuid(), QuestionId = questionId, IsCorrect = true }]
        };
        _quizRepositoryMock.Setup(repository => repository.GetQuestionsByLessonIdAsync(lessonId, default))
            .ReturnsAsync([new Question { Id = questionId, LessonId = lessonId, Explanation = "Correct." }]);
        _quizRepositoryMock.Setup(repository => repository.GetAttemptByClientIdAsync(userId, clientAttemptId, default))
            .ReturnsAsync(attempt);

        var result = await _quizService.SubmitQuizAsync(userId, new SubmitQuizRequest
        {
            LessonId = lessonId, ClientAttemptId = clientAttemptId, Answers = []
        });

        Assert.Equal(attempt.Id, result.AttemptId);
        Assert.True(result.Passed);
        Assert.True(result.Details.Single().IsCorrect);
        _quizRepositoryMock.Verify(repository => repository.AddAttemptAsync(It.IsAny<QuizAttempt>(), default), Times.Never);
    }

    [Fact]
    public async Task SubmitQuizAsync_WithCorrectAnswers_ShouldScoreAndAskJourneyToReconcile()
    {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        var q1Id = Guid.NewGuid();
        var optCorrectId = Guid.NewGuid();
        var q1 = new Question
        {
            Id = q1Id,
            LessonId = lessonId,
            Type = QuestionType.MultipleChoice,
            Prompt = "Choose correct answer",
            Options = new List<QuestionOption>
            {
                new QuestionOption { Id = optCorrectId, Content = "Correct", IsCorrect = true },
                new QuestionOption { Id = Guid.NewGuid(), Content = "Wrong", IsCorrect = false }
            }
        };

        var q2Id = Guid.NewGuid();
        var q2 = new Question
        {
            Id = q2Id,
            LessonId = lessonId,
            Type = QuestionType.FillInBlank,
            Prompt = "Type 'Apple'",
            ConfigJson = "{\"answers\":[\"apple\"]}"
        };

        _quizRepositoryMock.Setup(r => r.GetQuestionsByLessonIdAsync(lessonId, default))
            .ReturnsAsync(new List<Question> { q1, q2 });

        var submission = new SubmitQuizRequest
        {
            LessonId = lessonId,
            Answers = new List<QuizAnswerSubmission>
            {
                new QuizAnswerSubmission { QuestionId = q1Id, SelectedOptionId = optCorrectId },
                new QuizAnswerSubmission { QuestionId = q2Id, TextAnswer = "apple" }
            }
        };

        var result = await _quizService.SubmitQuizAsync(userId, submission);

        Assert.Equal(2, result.Score);
        Assert.Equal(2, result.MaxScore);
        Assert.True(result.Passed);

        _journeyMock.Verify(p => p.RefreshAsync(userId, lessonId, default), Times.Once);
        _progressRepositoryMock.Verify(p => p.UpsertProgressAsync(It.IsAny<LessonProgress>(), default), Times.Never);
    }

    [Fact]
    public async Task PassingQuizAgain_DoesNotCountAnotherCompletedLesson()
    {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var question = new Question
        {
            Id = Guid.NewGuid(), LessonId = lessonId, Type = QuestionType.MultipleChoice,
            Options = [new QuestionOption { Id = optionId, IsCorrect = true }]
        };
        _quizRepositoryMock.Setup(repository => repository.GetQuestionsByLessonIdAsync(lessonId, default))
            .ReturnsAsync([question]);
        _progressRepositoryMock.Setup(repository => repository.GetProgressAsync(userId, lessonId, default))
            .ReturnsAsync(new LessonProgress { UserId = userId, LessonId = lessonId, Status = "Completed" });

        await _quizService.SubmitQuizAsync(userId, new SubmitQuizRequest
        {
            LessonId = lessonId,
            Answers = [new QuizAnswerSubmission { QuestionId = question.Id, SelectedOptionId = optionId }]
        });

        _progressRepositoryMock.Verify(repository => repository.RecordLessonCompletionActivityAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task MatchingQuestion_HidesAnswerKeyAndScoresSubmittedPairs()
    {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var question = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            Type = QuestionType.Matching,
            Prompt = "Match words",
            ConfigJson = "{\"pairs\":{\"hello\":\"xin chào\",\"bye\":\"tạm biệt\"}}"
        };
        _quizRepositoryMock.Setup(repository => repository.GetQuestionsByLessonIdAsync(lessonId, default))
            .ReturnsAsync(new List<Question> { question });

        var displayed = await _quizService.GetLessonQuizAsync(lessonId);
        Assert.Equal("{}", displayed[0].ConfigJson);
        Assert.Equal(2, displayed[0].MatchingTerms.Count);
        Assert.Equal(2, displayed[0].MatchingChoices.Count);

        var result = await _quizService.SubmitQuizAsync(userId, new SubmitQuizRequest
        {
            LessonId = lessonId,
            Answers = new List<QuizAnswerSubmission>
            {
                new() { QuestionId = question.Id, MatchingJson = question.ConfigJson }
            }
        });

        Assert.Equal(1, result.Score);
        Assert.Equal(1, result.MaxScore);
    }
}
