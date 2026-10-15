
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

    [HttpGet]
    public async Task<IActionResult> GetDecks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "A valid user identity is required.");
        }

        var decks = await _deckService.GetDecksAsync(
            userId.Value,
            page,
            pageSize,
            cancellationToken);

        return Ok(decks);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDeckById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "A valid user identity is required.");
        }

        var deck = await _deckService.GetDeckByIdAsync(
            id, userId.Value, cancellationToken);

        if (deck == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Deck not found",
                detail: "The requested deck does not exist or is not accessible.");
        }

        return Ok(deck);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeck(
        [FromBody] CreateDeckRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "A valid user identity is required.");
        }

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
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid deck request",
                detail: ex.Message);
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateDeck(
        Guid id,
        [FromBody] UpdateDeckRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "A valid user identity is required.");
        }

        try
        {
            var deck = await _deckService.UpdateDeckAsync(
                id, request, userId.Value, cancellationToken);

            if (deck == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Deck not found",
                    detail: "The requested deck does not exist or is not accessible.");
            }

            return Ok(deck);
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid deck request",
                detail: ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDeck(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "A valid user identity is required.");
        }

        var deleted = await _deckService.DeleteDeckAsync(
            id, userId.Value, cancellationToken);

        if (!deleted)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Deck not found",
                detail: "The requested deck does not exist or is not accessible.");
        }

        return NoContent();
    }
}
