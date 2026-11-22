using LanguageLearning.Application.DTOs.Admin;
using LanguageLearning.Application.DTOs.Sharing;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ICurrentUserService _currentUserService;

    public AdminUsersController(IAdminService adminService, ICurrentUserService currentUserService)
    {
        _adminService = adminService;
        _currentUserService = currentUserService;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(List<AdminUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetUsersAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("users/{id:guid}/status")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.GetRequiredUserId();
        var result = await _adminService.UpdateUserStatusAsync(id, request.IsActive!.Value, actorId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("users/{id:guid}/role")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.GetRequiredUserId();
        var result = await _adminService.UpdateUserRoleAsync(id, request.Role, actorId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("public-decks")]
    [ProducesResponseType(typeof(List<PublicDeckResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicDecks(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetPublicDecksForModerationAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPatch("public-decks/{id:guid}/visibility")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDeckVisibility(Guid id, [FromBody] UpdateDeckVisibilityRequest request, CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.GetRequiredUserId();
        await _adminService.UpdateDeckVisibilityAsync(id, request.IsPublic!.Value, actorId, cancellationToken);
        return Ok(new { message = "Deck visibility successfully updated." });
    }

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(List<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetAuditLogsAsync(cancellationToken);
        return Ok(result);
    }
}

public class UpdateDeckVisibilityRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public bool? IsPublic { get; set; }
}
