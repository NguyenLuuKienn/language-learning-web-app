using LanguageLearning.Application.DTOs.Notebook;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notebook")]
public sealed class NotebookController : ControllerBase
{
    private readonly INotebookService _service;
    private readonly ICurrentUserService _currentUser;
    public NotebookController(INotebookService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetPageAsync(_currentUser.GetRequiredUserId(), search, page, pageSize, cancellationToken);
        return Ok(new { items = result.Items, total = result.Total, page = Math.Clamp(page, 1, 100000), pageSize = Math.Clamp(pageSize, 1, 50) });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NotebookEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(_currentUser.GetRequiredUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{entryId:guid}")]
    public async Task<IActionResult> Update(Guid entryId, [FromBody] NotebookEntryRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(_currentUser.GetRequiredUserId(), entryId, request, cancellationToken));

    [HttpDelete("{entryId:guid}")]
    public async Task<IActionResult> Delete(Guid entryId, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(_currentUser.GetRequiredUserId(), entryId, cancellationToken);
        return NoContent();
    }
}
