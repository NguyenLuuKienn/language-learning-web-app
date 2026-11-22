using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/lessons")]
[Authorize]
public class LessonsController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStudyTargetService _studyTargets;

    public LessonsController(
        ICourseService courseService,
        ICurrentUserService currentUserService,
        IStudyTargetService studyTargets)
    {
        _courseService = courseService;
        _currentUserService = currentUserService;
        _studyTargets = studyTargets;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LessonDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLessonDetail(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var target = isAdmin ? null : await _studyTargets.GetAsync(_currentUserService.GetRequiredUserId(), cancellationToken);
        var result = await _courseService.GetLessonDetailAsync(id, userId, cancellationToken, target, isAdmin);
        return Ok(result);
    }

    [HttpGet("{id:guid}/vocabulary")]
    [ProducesResponseType(typeof(List<VocabularyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLessonVocabulary(Guid id, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        var userId = _currentUserService.GetRequiredUserId();
        var target = isAdmin ? null : await _studyTargets.GetAsync(userId, cancellationToken);
        var lesson = await _courseService.GetLessonDetailAsync(id, userId, cancellationToken, target, isAdmin);
        return Ok(lesson.Vocabularies);
    }
}
