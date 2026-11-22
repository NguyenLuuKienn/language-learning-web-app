using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/admin/courses")]
[Authorize(Roles = "Admin")]
public class AdminCoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ICourseMaterialService _courseMaterialService;
    private readonly ISpecialPracticeService _specialPracticeService;
    private readonly ICurrentUserService _currentUserService;

    public AdminCoursesController(ICourseService courseService, ICourseMaterialService courseMaterialService, ISpecialPracticeService specialPracticeService, ICurrentUserService currentUserService)
    {
        _courseService = courseService;
        _courseMaterialService = courseMaterialService;
        _specialPracticeService = specialPracticeService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var result = await _courseService.CreateCourseAsync(request, cancellationToken);
        return Created($"/api/v1/courses/{result.Id}", result);
    }

    [HttpPut("{id:guid}/material")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ImportMaterial(Guid id, [FromBody] JsonElement material, CancellationToken cancellationToken)
    {
        await _courseMaterialService.ImportCourseMaterialAsync(id, _currentUserService.GetRequiredUserId(), material, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/special-practice")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSpecialPractice(Guid id, [FromBody] JsonElement content, CancellationToken cancellationToken)
    {
        await _specialPracticeService.UpdateContentAsync(id, content, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/special-practice/{kind:regex(^(certificate|interview)$)}")]
    public async Task<IActionResult> ListSpecialPractice(Guid id, string kind, CancellationToken cancellationToken) =>
        Ok(await _specialPracticeService.GetAdminContentAsync(id, kind, cancellationToken));

    [HttpPost("{id:guid}/special-practice/{kind:regex(^(certificate|interview)$)}")]
    public async Task<IActionResult> CreateSpecialPractice(Guid id, string kind, [FromBody] JsonElement item, CancellationToken cancellationToken) =>
        Ok(await _specialPracticeService.CreateAdminContentAsync(id, kind, item, cancellationToken));

    [HttpPost("{id:guid}/special-practice/{kind:regex(^(certificate|interview)$)}/import")]
    public async Task<IActionResult> ImportSpecialPractice(Guid id, string kind, [FromBody] JsonElement items, CancellationToken cancellationToken) =>
        Ok(await _specialPracticeService.ImportAdminContentAsync(id, kind, items, cancellationToken));

    [HttpPut("{id:guid}/special-practice/{kind:regex(^(certificate|interview)$)}/{itemId:guid}")]
    public async Task<IActionResult> EditSpecialPractice(Guid id, string kind, Guid itemId, [FromBody] JsonElement item, CancellationToken cancellationToken) =>
        Ok(await _specialPracticeService.UpdateAdminContentAsync(id, kind, itemId, item, cancellationToken));

    [HttpDelete("{id:guid}/special-practice/{kind:regex(^(certificate|interview)$)}/{itemId:guid}")]
    public async Task<IActionResult> DeleteSpecialPractice(Guid id, string kind, Guid itemId, CancellationToken cancellationToken)
    {
        await _specialPracticeService.DeleteAdminContentAsync(id, kind, itemId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lessons/{lessonId:guid}/material")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateLessonMaterial(Guid id, Guid lessonId, [FromBody] JsonElement lesson, CancellationToken cancellationToken)
    {
        await _courseMaterialService.UpdateLessonMaterialAsync(id, lessonId, lesson, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/lessons/{lessonId:guid}/remap-steps")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemapLessonSteps(Guid id, Guid lessonId, [FromBody] RemapLessonStepsRequest request, CancellationToken cancellationToken)
    {
        await _courseMaterialService.RemapLessonStepsAsync(id, lessonId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/placement-tests")]
    public async Task<IActionResult> GetPlacementTests(Guid id, CancellationToken cancellationToken) =>
        Ok(await _courseMaterialService.GetPlacementTestsAsync(id, cancellationToken));

    [HttpPut("{id:guid}/placement-tests")]
    public async Task<IActionResult> UpdatePlacementTests(Guid id, [FromBody] JsonElement tests, CancellationToken cancellationToken)
    {
        await _courseMaterialService.UpdatePlacementTestsAsync(id, tests, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCourse(Guid id, [FromBody] UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateCourseAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCourse(Guid id, CancellationToken cancellationToken)
    {
        await _courseService.DeleteCourseAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{courseId:guid}/units")]
    [ProducesResponseType(typeof(UnitResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUnit(Guid courseId, [FromBody] CreateUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await _courseService.CreateUnitAsync(courseId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("units/{unitId:guid}")]
    [ProducesResponseType(typeof(UnitResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUnit(Guid unitId, [FromBody] UpdateUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateUnitAsync(unitId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("units/{unitId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUnit(Guid unitId, CancellationToken cancellationToken)
    {
        await _courseService.DeleteUnitAsync(unitId, cancellationToken);
        return NoContent();
    }

    [HttpPost("units/{unitId:guid}/lessons")]
    [ProducesResponseType(typeof(LessonDetailResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLesson(Guid unitId, [FromBody] CreateLessonRequest request, CancellationToken cancellationToken)
    {
        var result = await _courseService.CreateLessonAsync(unitId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLesson(Guid lessonId, [FromBody] UpdateLessonRequest request, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateLessonAsync(lessonId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("lessons/{lessonId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteLesson(Guid lessonId, CancellationToken cancellationToken)
    {
        await _courseService.DeleteLessonAsync(lessonId, cancellationToken);
        return NoContent();
    }
}
