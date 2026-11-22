using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IQuizRepository
{
    Task<QuizAttempt?> GetAttemptByClientIdAsync(Guid userId, Guid clientAttemptId, CancellationToken cancellationToken = default);
    Task<List<Question>> GetQuestionsByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<bool> HasQuestionsAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<Question?> GetQuestionWithDetailsAsync(Guid questionId, CancellationToken cancellationToken = default);
    Task<bool> AddAttemptAsync(QuizAttempt attempt, CancellationToken cancellationToken = default);
    Task<List<QuizAttempt>> GetUserAttemptsAsync(Guid userId, Guid? lessonId = null, int limit = 50, CancellationToken cancellationToken = default);
}
