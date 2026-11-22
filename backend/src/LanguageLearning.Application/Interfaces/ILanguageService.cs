using LanguageLearning.Application.DTOs.Courses;

namespace LanguageLearning.Application.Interfaces;

public interface ILanguageService
{
    Task<List<LanguageResponse>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<LanguageResponse> CreateLanguageAsync(CreateLanguageRequest request, CancellationToken cancellationToken = default);
    Task<LanguageResponse> UpdateLanguageAsync(Guid id, UpdateLanguageRequest request, CancellationToken cancellationToken = default);
    Task DeleteLanguageAsync(Guid id, CancellationToken cancellationToken = default);
}
