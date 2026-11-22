using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/vocabularies")]
[Authorize]
public class VocabulariesController : ControllerBase
{
    private readonly IVocabularyService _vocabularyService;

    public VocabulariesController(IVocabularyService vocabularyService)
    {
        _vocabularyService = vocabularyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<VocabularyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] Guid? languageId, [FromQuery] string? term, CancellationToken cancellationToken)
    {
        var result = await _vocabularyService.SearchAsync(languageId, term, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VocabularyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vocabularyService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VocabularyResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateVocabularyRequest request, CancellationToken cancellationToken)
    {
        var result = await _vocabularyService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VocabularyResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVocabularyRequest request, CancellationToken cancellationToken)
    {
        var result = await _vocabularyService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _vocabularyService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
