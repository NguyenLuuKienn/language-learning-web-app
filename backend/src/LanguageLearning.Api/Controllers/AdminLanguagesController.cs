using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/admin/languages")]
[Authorize(Roles = "Admin")]
public class AdminLanguagesController : ControllerBase
{
    private readonly ILanguageService _languageService;

    public AdminLanguagesController(ILanguageService languageService)
    {
        _languageService = languageService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LanguageResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLanguage([FromBody] CreateLanguageRequest request, CancellationToken cancellationToken)
    {
        var result = await _languageService.CreateLanguageAsync(request, cancellationToken);
        return Created($"/api/v1/languages/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LanguageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLanguage(Guid id, [FromBody] UpdateLanguageRequest request, CancellationToken cancellationToken)
    {
        var result = await _languageService.UpdateLanguageAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteLanguage(Guid id, CancellationToken cancellationToken)
    {
        await _languageService.DeleteLanguageAsync(id, cancellationToken);
        return NoContent();
    }
}
