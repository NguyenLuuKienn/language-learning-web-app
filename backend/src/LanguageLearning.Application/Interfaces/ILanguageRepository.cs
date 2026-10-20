
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface ILanguageRepository
{
    Task<List<Language>> GetActiveLanguagesAsync(
        CancellationToken cancellationToken = default);
}
