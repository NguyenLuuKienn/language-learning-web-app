using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/learning-journey")]
public sealed class LessonJourneyController(ILessonJourneyService journey, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("lessons/{lessonId:guid}")]
    public async Task<ActionResult<LessonJourneyResponse>> Get(Guid lessonId, CancellationToken cancellationToken) =>
        Ok(await journey.GetAsync(currentUser.GetRequiredUserId(), lessonId, cancellationToken));

    [HttpPost("lessons/{lessonId:guid}/refresh")]
    public async Task<ActionResult<LessonJourneyResponse>> Refresh(Guid lessonId, CancellationToken cancellationToken) =>
        Ok(await journey.RefreshAsync(currentUser.GetRequiredUserId(), lessonId, cancellationToken));

    [HttpPut("lessons/{lessonId:guid}/steps/{stepId}")]
    public async Task<ActionResult<LessonJourneyResponse>> Visit(Guid lessonId, string stepId,
        [FromBody] SaveLessonJourneyStepRequest request, CancellationToken cancellationToken) =>
        Ok(await journey.VisitAsync(currentUser.GetRequiredUserId(), lessonId, stepId, request, cancellationToken));
}
