using LanguageLearning.Application.Common;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStudyTargetService _studyTargetService;
    private readonly IWritingSubmissionService _writingSubmissionService;

    public MeController(IAuthService authService, ICurrentUserService currentUserService, IStudyTargetService studyTargetService, IWritingSubmissionService writingSubmissionService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _studyTargetService = studyTargetService;
        _writingSubmissionService = writingSubmissionService;
    }

    [HttpGet("writing-submissions/latest")]
    [ProducesResponseType(typeof(WritingSubmissionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestWriting([FromQuery] string promptId, CancellationToken cancellationToken)
    {
        var submission = await _writingSubmissionService.GetLatestAsync(_currentUserService.GetRequiredUserId(), promptId, cancellationToken);
        return Ok(submission);
    }

    [HttpPost("writing-submissions")]
    [ProducesResponseType(typeof(WritingSubmissionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveWriting([FromBody] SaveWritingSubmissionRequest request, CancellationToken cancellationToken)
    {
        var submission = await _writingSubmissionService.SaveAsync(_currentUserService.GetRequiredUserId(), request, cancellationToken);
        return Ok(submission);
    }

    [HttpGet("study-target")]
    [ProducesResponseType(typeof(StudyTargetDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudyTarget(CancellationToken cancellationToken)
    {
        var target = await _studyTargetService.GetAsync(_currentUserService.GetRequiredUserId(), cancellationToken);
        return Ok(target);
    }

    [HttpPut("study-target")]
    [ProducesResponseType(typeof(StudyTargetDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveStudyTarget([FromBody] StudyTargetDto request, CancellationToken cancellationToken)
    {
        var target = await _studyTargetService.SaveAsync(_currentUserService.GetRequiredUserId(), request, cancellationToken);
        return Ok(target);
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var profile = await _authService.GetProfileAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPatch]
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var profile = await _authService.UpdateProfileAsync(userId, request, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        return Ok(new { message = Messages.Auth.PasswordChanged });
    }

    [HttpGet("daily-goal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyGoal(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var profile = await _authService.GetProfileAsync(userId, cancellationToken);
        return Ok(new { dailyTarget = profile.DailyTarget });
    }

    [HttpPatch("daily-goal")]
    [HttpPut("daily-goal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDailyGoal([FromBody] UpdateDailyGoalRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _authService.UpdateDailyGoalAsync(userId, request.DailyTarget, cancellationToken);
        return Ok(new { dailyTarget = updated, message = Messages.Auth.DailyGoalUpdated });
    }
}
