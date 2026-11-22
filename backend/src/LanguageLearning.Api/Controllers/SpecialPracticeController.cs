using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/special-practice")]
public sealed class SpecialPracticeController(ISpecialPracticeService service, ICurrentUserService currentUser, IMediaService media) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken) =>
        Ok(await service.GetCatalogAsync(currentUser.GetRequiredUserId(), cancellationToken));

    [HttpPost("attempts")]
    public async Task<IActionResult> Start([FromBody] StartSpecialPracticeRequest request, CancellationToken cancellationToken) =>
        Ok(await service.StartAsync(currentUser.GetRequiredUserId(), request, cancellationToken));

    [HttpGet("attempts/{attemptId:guid}")]
    public async Task<IActionResult> Get(Guid attemptId, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(currentUser.GetRequiredUserId(), attemptId, cancellationToken));

    [HttpPut("attempts/{attemptId:guid}/answers")]
    public async Task<IActionResult> Answer(Guid attemptId, [FromBody] SaveSpecialPracticeAnswerRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SaveAnswerAsync(currentUser.GetRequiredUserId(), attemptId, request, cancellationToken));

    [HttpPost("attempts/{attemptId:guid}/questions/{questionId:guid}/audio")]
    [EnableRateLimiting("upload")]
    public async Task<IActionResult> UploadAudio(Guid attemptId, Guid questionId, IFormFile file, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var attempt = await service.GetAsync(userId, attemptId, cancellationToken);
        if (attempt.Status != "InProgress" || attempt.Questions.All(question => question.Id != questionId || question.Skill != "speaking"))
            return BadRequest(new { message = "An active speaking question is required." });
        if (file == null || file.Length == 0 || file.ContentType is not ("audio/webm" or "audio/ogg" or "audio/wav" or "audio/mpeg"))
            return BadRequest(new { message = "Upload a supported audio recording." });
        using var stream = file.OpenReadStream();
        var uploaded = await media.UploadAsync(stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        return Ok(await service.SaveAudioAsync(userId, attemptId, questionId, uploaded.Url, cancellationToken));
    }

    [HttpPost("attempts/{attemptId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid attemptId, CancellationToken cancellationToken) =>
        Ok(await service.SubmitAsync(currentUser.GetRequiredUserId(), attemptId, cancellationToken));

    [HttpGet("reviews/pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PendingReviews(CancellationToken cancellationToken) =>
        Ok(await service.GetPendingReviewsAsync(cancellationToken));

    [HttpGet("reviews/{attemptId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Review(Guid attemptId, CancellationToken cancellationToken) =>
        Ok(await service.GetReviewAsync(attemptId, cancellationToken));

    [HttpPost("reviews/{attemptId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Grade(Guid attemptId, [FromBody] GradeSpecialPracticeRequest request, CancellationToken cancellationToken) =>
        Ok(await service.GradeAsync(attemptId, request, cancellationToken));
}
