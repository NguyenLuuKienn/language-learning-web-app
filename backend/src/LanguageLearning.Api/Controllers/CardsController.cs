using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;
    private readonly ICurrentUserService _currentUserService;

    public CardsController(ICardService cardService, ICurrentUserService currentUserService)
    {
        _cardService = cardService;
        _currentUserService = currentUserService;
    }

    [HttpGet("decks/{deckId:guid}/cards")]
    [ProducesResponseType(typeof(List<CardResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCardsByDeckId(Guid deckId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var cards = await _cardService.GetCardsByDeckIdAsync(deckId, userId, cancellationToken);
        return Ok(cards);
    }

    [HttpPost("decks/{deckId:guid}/cards")]
    [ProducesResponseType(typeof(CardResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCard(Guid deckId, [FromBody] CreateCardRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _cardService.CreateCardAsync(deckId, request, userId, cancellationToken);
        return Created($"/api/v1/cards/{result.Id}", result);
    }

    [HttpPost("decks/{deckId:guid}/cards/batch")]
    [ProducesResponseType(typeof(List<CardResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBatchCards(Guid deckId, [FromBody] BatchCreateCardsRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _cardService.CreateBatchCardsAsync(deckId, request, userId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("cards/{id:guid}")]
    [ProducesResponseType(typeof(CardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCardById(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var result = await _cardService.GetCardByIdAsync(id, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("cards/{id:guid}")]
    [ProducesResponseType(typeof(CardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCard(Guid id, [FromBody] UpdateCardRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _cardService.UpdateCardAsync(id, request, userId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("cards/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCard(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _cardService.DeleteCardAsync(id, userId, cancellationToken);
        return NoContent();
    }
}
