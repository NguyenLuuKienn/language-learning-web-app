using LanguageLearning.Application.DTOs.Csv;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/decks/{id:guid}")]
[Authorize]
public class DeckCsvController : ControllerBase
{
    private readonly ICsvService _csvService;
    private readonly ICurrentUserService _currentUserService;

    public DeckCsvController(ICsvService csvService, ICurrentUserService currentUserService)
    {
        _csvService = csvService;
        _currentUserService = currentUserService;
    }

    [HttpPost("imports/preview")]
    [ProducesResponseType(typeof(CsvPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewCsv([FromRoute] Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No CSV file uploaded." });
        }

        using var stream = file.OpenReadStream();
        var result = await _csvService.PreviewAsync(stream, cancellationToken);
        return Ok(result);
    }

    [HttpPost("imports")]
    [ProducesResponseType(typeof(CsvImportResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CommitImport([FromRoute] Guid id, [FromBody] CommitCsvImportRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _csvService.ImportAsync(id, request, userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var bytes = await _csvService.ExportDeckAsync(id, userId, cancellationToken);
        return File(bytes, "text/csv; charset=utf-8", $"deck-{id:N}.csv");
    }
}
