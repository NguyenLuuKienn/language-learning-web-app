using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IVocabularyRepository
{
    Task<List<Vocabulary>> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<List<Vocabulary>> SearchAsync(Guid? languageId, string? term, int limit = 50, CancellationToken cancellationToken = default, string? level = null, IReadOnlyCollection<string>? domains = null);
    Task<Vocabulary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> GetLessonLanguageIdAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task AddAsync(Vocabulary vocabulary, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vocabulary vocabulary, CancellationToken cancellationToken = default);
    Task DeleteAsync(Vocabulary vocabulary, CancellationToken cancellationToken = default);
}
