
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/languages")]
[Authorize]
public class LanguagesController : ControllerBase
{
    private readonly ILanguageService _languageService;

    public LanguagesController(ILanguageService languageService)
    {
        _languageService = languageService;
    }

    [HttpGet]
    public async Task<ActionResult<List<LanguageOptionResponse>>> GetLanguages(
        CancellationToken cancellationToken)
    {
        var languages = await _languageService
            .GetActiveLanguagesAsync(cancellationToken);

        return Ok(languages);
    }
}
