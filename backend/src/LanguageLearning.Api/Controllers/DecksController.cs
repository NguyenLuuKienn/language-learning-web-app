
using System.Security.Claims;
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/decks")]
[Authorize]
public class DecksController : ControllerBase
{
    private readonly IDeckService _deckService;

    public DecksController(IDeckService deckService)
    {
        _deckService = deckService;
    }

    // Get authenticated user ID from claims
    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdValue))
            return null;

        return Guid.TryParse(userIdValue, out var userId)
            && userId != Guid.Empty
            ? userId
            : null;
    }

    // GET: api/v1/decks
    [HttpGet]
    public async Task<IActionResult> GetDecks(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var decks = await _deckService.GetDecksAsync(
            userId.Value, cancellationToken);

        return Ok(decks);
    }

    // GET: api/v1/decks/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDeckById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var deck = await _deckService.GetDeckByIdAsync(
            id, userId.Value, cancellationToken);

        if (deck == null)
            return NotFound();

        return Ok(deck);
    }

    // POST: api/v1/decks
    [HttpPost]
    public async Task<IActionResult> CreateDeck(
        [FromBody] CreateDeckRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        try
        {
            var deck = await _deckService.CreateDeckAsync(
                request, userId.Value, cancellationToken);

            return CreatedAtAction(
                nameof(GetDeckById),
                new { id = deck.Id },
                deck);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PATCH: api/v1/decks/{id}
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateDeck(
        Guid id,
        [FromBody] UpdateDeckRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        try
        {
            var deck = await _deckService.UpdateDeckAsync(
                id, request, userId.Value, cancellationToken);

            if (deck == null)
                return NotFound();

            return Ok(deck);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/v1/decks/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDeck(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var deleted = await _deckService.DeleteDeckAsync(
            id, userId.Value, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
