using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/admin/curriculum")]
[Authorize(Roles = "Admin")]
public class AdminCurriculumController : ControllerBase
{
    private readonly ICourseService _courseService;

    public AdminCurriculumController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(CurriculumExportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCurriculum([FromQuery] Guid? languageId, CancellationToken cancellationToken)
    {
        var result = await _courseService.ExportCurriculumAsync(languageId, cancellationToken);
        return Ok(result);
    }

}
