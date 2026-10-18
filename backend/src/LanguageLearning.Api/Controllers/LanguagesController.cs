
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/languages")]
[Authorize]
public class LanguagesController : ControllerBase
{
    private readonly IDeckService _deckService;

    public LanguagesController(IDeckService deckService)
    {
        _deckService = deckService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLanguages(
        CancellationToken cancellationToken)
    {
        var languages = await _deckService.GetActiveLanguagesAsync(
            cancellationToken);

        return Ok(languages);
    }
}
