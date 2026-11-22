using LanguageLearning.Application.DTOs.VocabularyPractice;

namespace LanguageLearning.Application.Interfaces;

public interface IVocabularySessionService
{
    Task<List<VocabularyLessonChoice>> GetAvailableAsync(Guid userId, CancellationToken cancellationToken);
    Task<VocabularySessionResponse> StartAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken);
    Task<VocabularySessionResponse> GetAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
    Task<VocabularyAnswerResponse> AnswerAsync(Guid userId, Guid sessionId, VocabularyPracticeAnswerRequest request, CancellationToken cancellationToken);
    Task<VocabularySessionResponse> CompleteAfterPerfectRoundAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
