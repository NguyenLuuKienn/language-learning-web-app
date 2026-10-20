
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Application.Interfaces;

namespace LanguageLearning.Application.Services;

public class LanguageService : ILanguageService
{
    private readonly ILanguageRepository _languageRepository;

    public LanguageService(ILanguageRepository languageRepository)
    {
        _languageRepository = languageRepository;
    }

    public async Task<List<LanguageOptionResponse>> GetActiveLanguagesAsync(
        CancellationToken cancellationToken = default)
    {
        var languages = await _languageRepository
            .GetActiveLanguagesAsync(cancellationToken);

        return languages
            .Select(language => new LanguageOptionResponse(
                language.Id,
                language.Name))
            .ToList();
    }
}
