
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/reviews")]
[Authorize]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("due")]
    public async Task<IActionResult> GetDueCards(
        [FromQuery] Guid deckId,
        CancellationToken cancellationToken)
    {
        var cards = await _reviewService.GetDueCardsAsync(
            deckId,
            cancellationToken);

        return Ok(cards);
    }
}
