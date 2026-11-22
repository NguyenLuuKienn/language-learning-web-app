using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/progress")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progressService;
    private readonly ICurrentUserService _currentUserService;

    public ProgressController(IProgressService progressService, ICurrentUserService currentUserService)
    {
        _progressService = progressService;
        _currentUserService = currentUserService;
    }

    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonProgressResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLessonProgress(Guid lessonId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _progressService.GetLessonProgressAsync(userId, lessonId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonProgressResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLessonProgress(Guid lessonId, [FromBody] UpdateProgressRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _progressService.UpdateLessonProgressAsync(userId, lessonId, request.Status, cancellationToken);
        return Ok(result);
    }

    [HttpGet("courses/{courseId:guid}")]
    [ProducesResponseType(typeof(CourseProgressResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseProgress(Guid courseId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _progressService.GetCourseProgressAsync(userId, courseId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("resume")]
    [ProducesResponseType(typeof(ResumeLearningResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResumeLearning(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _progressService.GetResumeLearningAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("roadmap")]
    [ProducesResponseType(typeof(LearningRoadmapResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoadmap(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        return Ok(await _progressService.GetRoadmapAsync(userId, cancellationToken));
    }
}
