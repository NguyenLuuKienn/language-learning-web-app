using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/placement")]
public sealed class PlacementController : ControllerBase
{
    private readonly IPlacementService _placement;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediaService _media;

    public PlacementController(IPlacementService placement, ICurrentUserService currentUser, IMediaService media)
    {
        _placement = placement;
        _currentUser = currentUser;
        _media = media;
    }

    [HttpGet("courses/{courseId:guid}")]
    public async Task<PlacementExamDto> Get(Guid courseId, CancellationToken cancellationToken) =>
        await _placement.GetExamAsync(courseId, cancellationToken);

    [HttpPost("courses/{courseId:guid}/submit")]
    [EnableRateLimiting("placement")]
    public async Task<PlacementResultDto> Submit(Guid courseId, [FromBody] PlacementSubmissionDto submission, CancellationToken cancellationToken) =>
        await _placement.SubmitAsync(_currentUser.GetRequiredUserId(), courseId, submission, cancellationToken);

    [HttpPost("courses/{courseId:guid}/audio/{questionId}")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("placement")]
    public async Task<IActionResult> UploadAudio(Guid courseId, string questionId, IFormFile file, CancellationToken cancellationToken)
    {
        var exam = await _placement.GetExamAsync(courseId, cancellationToken);
        if (!exam.Questions.Any(question => question.Id == questionId && question.Section == "speaking"))
            return BadRequest(new { message = "Invalid speaking question." });
        if (file == null || file.Length < 1024 || file.Length > 10 * 1024 * 1024 || file.ContentType != "audio/webm")
            return BadRequest(new { message = "A WebM audio recording between 1 KB and 10 MB is required." });
        using var stream = file.OpenReadStream();
        var signature = new byte[4];
        if (await stream.ReadAsync(signature, cancellationToken) != 4 ||
            !signature.SequenceEqual(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }))
            return BadRequest(new { message = "Invalid WebM audio file." });
        stream.Position = 0;
        var uploaded = await _media.UploadAsync(stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        await _placement.RegisterAudioAsync(_currentUser.GetRequiredUserId(), courseId, questionId, uploaded.FileName, cancellationToken);
        return Ok(uploaded);
    }

    [HttpGet("attempts/{attemptId:guid}")]
    public async Task<PlacementResultDto> GetResult(Guid attemptId, CancellationToken cancellationToken) =>
        await _placement.GetResultAsync(_currentUser.GetRequiredUserId(), attemptId, cancellationToken);

    [HttpGet("courses/{courseId:guid}/latest")]
    public async Task<PlacementResultDto?> GetLatest(Guid courseId, CancellationToken cancellationToken) =>
        await _placement.GetLatestResultAsync(_currentUser.GetRequiredUserId(), courseId, cancellationToken);

    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin")]
    public async Task<List<PlacementReviewDto>> GetPending(CancellationToken cancellationToken) =>
        await _placement.GetPendingAsync(cancellationToken);

    [HttpPost("admin/attempts/{attemptId:guid}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<PlacementResultDto> Review(Guid attemptId, [FromBody] PlacementReviewRequestDto review, CancellationToken cancellationToken) =>
        await _placement.ReviewAsync(_currentUser.GetRequiredUserId(), attemptId, review, cancellationToken);
}
