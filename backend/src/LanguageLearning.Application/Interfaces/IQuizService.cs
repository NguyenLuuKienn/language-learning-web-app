using LanguageLearning.Application.DTOs.Quiz;

namespace LanguageLearning.Application.Interfaces;

public interface IQuizService
{
    Task<List<QuizQuestionResponse>> GetLessonQuizAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<QuizResultResponse> SubmitQuizAsync(Guid userId, SubmitQuizRequest request, CancellationToken cancellationToken = default);
    Task<List<QuizAttemptHistoryResponse>> GetAttemptHistoryAsync(Guid userId, Guid? lessonId = null, CancellationToken cancellationToken = default);
}
