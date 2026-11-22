using LanguageLearning.Application.Common;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ILanguageService _languageService;
    private readonly ICourseMaterialService _courseMaterialService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStudyTargetService _studyTargets;

    public CoursesController(ICourseService courseService, ILanguageService languageService, ICourseMaterialService courseMaterialService, ICurrentUserService currentUserService, IStudyTargetService studyTargets)
    {
        _courseService = courseService;
        _languageService = languageService;
        _courseMaterialService = courseMaterialService;
        _currentUserService = currentUserService;
        _studyTargets = studyTargets;
    }

    [HttpGet("languages")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<LanguageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLanguages(CancellationToken cancellationToken)
    {
        var result = await _languageService.GetLanguagesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("courses")]
    [ProducesResponseType(typeof(List<CourseResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] Guid? languageId, [FromQuery] string? level, [FromQuery] string? domain, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var result = await _courseService.GetCoursesAsync(languageId, level, domain, userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("courses/{id:guid}")]
    [ProducesResponseType(typeof(CourseDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseDetail(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var target = isAdmin ? null : await _studyTargets.GetAsync(_currentUserService.GetRequiredUserId(), cancellationToken);
        var result = await _courseService.GetCourseDetailAsync(id, userId, cancellationToken, target, isAdmin);
        return Ok(result);
    }

    [HttpGet("courses/{id:guid}/material")]
    public async Task<IActionResult> GetCourseMaterial(Guid id, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        var target = isAdmin ? null : await _studyTargets.GetAsync(_currentUserService.GetRequiredUserId(), cancellationToken);
        var material = await _courseMaterialService.GetCourseMaterialAsync(id, target, isAdmin, cancellationToken);
        return material.HasValue ? Ok(material.Value) : NoContent();
    }

    [HttpPost("courses/{id:guid}/enroll")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnrollCourse(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var isAdmin = User.IsInRole("Admin");
        var target = isAdmin ? null : await _studyTargets.GetAsync(userId, cancellationToken);
        await _courseService.EnrollCourseAsync(userId, id, cancellationToken, target, isAdmin);
        return Ok(new { message = Messages.Course.EnrollmentSuccess });
    }
}
