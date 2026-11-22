using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly ISrsService _srsService;
    private readonly ICurrentUserService _currentUserService;

    public ReviewsController(ISrsService srsService, ICurrentUserService currentUserService)
    {
        _srsService = srsService;
        _currentUserService = currentUserService;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(ReviewOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview([FromQuery] Guid? deckId, [FromQuery] Guid? lessonId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        return Ok(await _srsService.GetOverviewAsync(userId, cancellationToken, deckId, lessonId));
    }

    [HttpGet("lessons")]
    [ProducesResponseType(typeof(List<ReviewLessonSourceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLessonSources(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        return Ok(await _srsService.GetLessonSourcesAsync(userId, cancellationToken));
    }

    [HttpGet("due")]
    [ProducesResponseType(typeof(DueCardsSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDueCards([FromQuery] Guid? deckId, [FromQuery] Guid? lessonId, [FromQuery] int limit = 50, [FromQuery] bool dueOnly = false, [FromQuery] bool newOnly = false, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _srsService.GetDueCardsAsync(userId, deckId, limit, cancellationToken, dueOnly, newOnly, lessonId);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReviewResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _srsService.SubmitReviewAsync(userId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(List<ReviewResultResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _srsService.GetHistoryAsync(userId, limit, cancellationToken);
        return Ok(result);
    }
}
