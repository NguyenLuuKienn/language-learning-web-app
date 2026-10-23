
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/decks/{deckId:guid}/cards")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;

    public CardsController(ICardService cardService)
    {
        _cardService = cardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCards(
        Guid deckId,
        CancellationToken cancellationToken)
    {
        var cards = await _cardService.GetCardsAsync(
            deckId,
            cancellationToken);

        return Ok(cards);
    }

    [HttpGet("{cardId:guid}")]
    public async Task<IActionResult> GetCardById(
        Guid deckId,
        Guid cardId,
        CancellationToken cancellationToken)
    {
        var card = await _cardService.GetCardByIdAsync(
            deckId,
            cardId,
            cancellationToken);

        return Ok(card);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCard(
        Guid deckId,
        [FromBody] CreateCardRequest request,
        CancellationToken cancellationToken)
    {
        var card = await _cardService.CreateCardAsync(
            deckId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetCardById),
            new { deckId, cardId = card.Id },
            card);
    }

    [HttpPatch("{cardId:guid}")]
    public async Task<IActionResult> UpdateCard(
        Guid deckId,
        Guid cardId,
        [FromBody] UpdateCardRequest request,
        CancellationToken cancellationToken)
    {
        var card = await _cardService.UpdateCardAsync(
            deckId,
            cardId,
            request,
            cancellationToken);

        return Ok(card);
    }

    [HttpDelete("{cardId:guid}")]
    public async Task<IActionResult> DeleteCard(
        Guid deckId,
        Guid cardId,
        CancellationToken cancellationToken)
    {
        await _cardService.DeleteCardAsync(
            deckId,
            cardId,
            cancellationToken);

        return NoContent();
    }
}
