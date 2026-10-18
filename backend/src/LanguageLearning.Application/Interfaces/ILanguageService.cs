
using LanguageLearning.Application.DTOs.Decks;

namespace LanguageLearning.Application.Interfaces;

public interface ILanguageService
{
    Task<List<LanguageOptionResponse>> GetActiveLanguagesAsync(
        CancellationToken cancellationToken = default);
}
