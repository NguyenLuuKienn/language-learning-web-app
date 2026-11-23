using LanguageLearning.Api.Authorization;
using LanguageLearning.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public class MediaController : ControllerBase
{
    private readonly MediaService _media;
    private readonly IConfiguration _configuration;

    public MediaController(MediaService media, IConfiguration configuration)
    {
        _media = media;
        _configuration = configuration;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MediaService.MaxAudioBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null) throw new ArgumentException("A file is required.");
        await using var stream = file.OpenReadStream();
        var result = await _media.UploadAsync(stream, file.ContentType, file.Length, ct);
        var baseUrl = _configuration["Media:PublicBaseUrl"]?.TrimEnd('/') ??
            $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var url = $"{baseUrl}{result.Url}";
        return Created(url, result with { Url = url });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var media = await _media.GetAsync(id, ct);
        if (media is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(media.Content, media.ContentType);
    }
}
