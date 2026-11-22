using LanguageLearning.Application.DTOs.Courses;

namespace LanguageLearning.Application.Interfaces;

public interface IVocabularyService
{
    Task<List<VocabularyResponse>> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<List<VocabularyResponse>> SearchAsync(Guid? languageId, string? term, CancellationToken cancellationToken = default);
    Task<VocabularyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VocabularyResponse> CreateAsync(CreateVocabularyRequest request, CancellationToken cancellationToken = default);
    Task<VocabularyResponse> UpdateAsync(Guid id, UpdateVocabularyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
