
using LanguageLearning.Application.Common;
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

    [HttpGet]
    public async Task<IActionResult> GetDecks(
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        var decks = await _deckService.GetDecksAsync(
            pagination,
            cancellationToken);

        return Ok(decks);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDeckById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deck = await _deckService.GetDeckByIdAsync(
            id,
            cancellationToken);

        return Ok(deck);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeck(
        [FromBody] CreateDeckRequest request,
        CancellationToken cancellationToken)
    {
        var deck = await _deckService.CreateDeckAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetDeckById),
            new { id = deck.Id },
            deck);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateDeck(
        Guid id,
        [FromBody] UpdateDeckRequest request,
        CancellationToken cancellationToken)
    {
        var deck = await _deckService.UpdateDeckAsync(
            id,
            request,
            cancellationToken);

        return Ok(deck);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDeck(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _deckService.DeleteDeckAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}
