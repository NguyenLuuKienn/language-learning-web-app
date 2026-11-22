using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/admin/srs")]
[Authorize(Roles = "Admin")]
public sealed class AdminSrsController(IReviewRepository reviews) : ControllerBase
{
    [HttpGet("evaluation-readiness")]
    public async Task<IActionResult> GetEvaluationReadiness(CancellationToken cancellationToken) =>
        Ok(await reviews.GetEvaluationReadinessAsync(cancellationToken));
}
