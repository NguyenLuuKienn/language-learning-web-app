using LanguageLearning.Application.DTOs.Media;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;

    public MediaController(IMediaService mediaService, IStudyAccessService access, ICurrentUserService currentUser)
    {
        _mediaService = mediaService;
        _access = access;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("upload")]
    [ProducesResponseType(typeof(MediaUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        using var stream = file.OpenReadStream();
        var result = await _mediaService.UploadAsync(stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{fileName}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(string fileName, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdmin() && !await _access.CanAccessMediaAsync(
                _currentUser.GetRequiredUserId(), fileName, cancellationToken))
            return NotFound(new { message = "File not found." });
        var file = await _mediaService.GetFileAsync(fileName, cancellationToken);
        if (file == null)
        {
            return NotFound(new { message = "File not found." });
        }

        return File(file.Value.stream, file.Value.contentType, enableRangeProcessing: true);
    }
}
