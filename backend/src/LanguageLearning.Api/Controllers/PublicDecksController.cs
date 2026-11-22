using LanguageLearning.Application.DTOs.Sharing;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/public/decks")]
[Authorize]
public class PublicDecksController : ControllerBase
{
    private readonly ISharingService _sharingService;
    private readonly ICurrentUserService _currentUserService;

    public PublicDecksController(ISharingService sharingService, ICurrentUserService currentUserService)
    {
        _sharingService = sharingService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PublicDeckResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicDecks([FromQuery] Guid? languageId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await _sharingService.GetPublicDecksAsync(languageId, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType(typeof(PublicDeckPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicDeckPreview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sharingService.GetPublicDeckPreviewAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/clone")]
    [Authorize]
    [ProducesResponseType(typeof(CloneDeckResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloneDeck(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _sharingService.CloneDeckAsync(id, userId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
