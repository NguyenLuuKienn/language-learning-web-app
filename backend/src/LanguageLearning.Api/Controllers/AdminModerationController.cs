using LanguageLearning.Api.Authorization;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/admin/moderation")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminModerationController(AdminModerationService moderation) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> ListUsers([FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct) =>
        Ok(await moderation.ListUsersAsync(pagination, search, ct));

    [HttpPost("users/{id:guid}/lock")]
    public async Task<IActionResult> LockUser(Guid id, CancellationToken ct) => Ok(await moderation.SetUserActiveAsync(id, false, ct));

    [HttpPost("users/{id:guid}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid id, CancellationToken ct) => Ok(await moderation.SetUserActiveAsync(id, true, ct));

    [HttpGet("decks")]
    public async Task<IActionResult> ListDecks([FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct) =>
        Ok(await moderation.ListDecksAsync(pagination, search, ct));

    [HttpPost("decks/{id:guid}/hide")]
    public async Task<IActionResult> HideDeck(Guid id, CancellationToken ct) => Ok(await moderation.SetDeckHiddenAsync(id, true, ct));

    [HttpPost("decks/{id:guid}/restore")]
    public async Task<IActionResult> RestoreDeck(Guid id, CancellationToken ct) => Ok(await moderation.SetDeckHiddenAsync(id, false, ct));
}
