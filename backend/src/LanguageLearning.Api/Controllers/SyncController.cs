using LanguageLearning.Application.DTOs.Sync;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/sync")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ICurrentUserService _currentUserService;

    public SyncController(ISyncService syncService, ICurrentUserService currentUserService)
    {
        _syncService = syncService;
        _currentUserService = currentUserService;
    }

    [HttpPost("push")]
    [ProducesResponseType(typeof(SyncPushResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Push([FromBody] SyncPushRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _syncService.PushOperationsAsync(userId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("pull")]
    [ProducesResponseType(typeof(SyncPullResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pull([FromQuery] DateTime? cursor, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _syncService.PullChangesAsync(userId, cursor, cancellationToken);
        return Ok(result);
    }
}
