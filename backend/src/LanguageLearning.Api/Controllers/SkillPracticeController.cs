using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/skill-practice")]
public sealed class SkillPracticeController(ISkillPracticeService service, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("{kind}")]
    public async Task<ActionResult<List<SkillPracticeResponse>>> Get(string kind, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(currentUser.GetRequiredUserId(), kind, cancellationToken));

    [HttpPost("{kind}")]
    public async Task<ActionResult<SkillPracticeResponse>> Record(string kind, [FromBody] SkillPracticeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.RecordAsync(currentUser.GetRequiredUserId(), kind, request, cancellationToken));
}
