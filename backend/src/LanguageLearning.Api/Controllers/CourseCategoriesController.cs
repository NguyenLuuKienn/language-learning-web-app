using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

public record CourseCategoryRequest(string Kind, string Framework, string Code, string TitleVi, string TitleEn, string DescriptionVi, string DescriptionEn, int SortOrder);

[ApiController]
[Route("api/v1/course-categories")]
[Authorize]
public sealed class CourseCategoriesController(ICourseCategoryRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await repository.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CourseCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalize(request, out var normalized, out var error)) return BadRequest(error);
        if (await repository.ExistsAsync(normalized.Kind, normalized.Framework, normalized.Code, cancellationToken))
            return Conflict("Category code already exists in this group.");
        var category = new CourseCategory { Id = Guid.NewGuid(), Kind = normalized.Kind, Framework = normalized.Framework,
            Code = normalized.Code, TitleVi = normalized.TitleVi, TitleEn = normalized.TitleEn,
            DescriptionVi = normalized.DescriptionVi, DescriptionEn = normalized.DescriptionEn, SortOrder = normalized.SortOrder };
        await repository.AddAsync(category, cancellationToken);
        return Created($"/api/v1/course-categories/{category.Id}", category);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CourseCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalize(request, out var normalized, out var error)) return BadRequest(error);
        var category = await repository.GetAsync(id, cancellationToken);
        if (category is null) return NotFound();
        if (category.Kind != normalized.Kind || category.Framework != normalized.Framework || category.Code != normalized.Code)
            return BadRequest("Category identity cannot be changed. Create a new category instead.");
        category.TitleVi = normalized.TitleVi; category.TitleEn = normalized.TitleEn;
        category.DescriptionVi = normalized.DescriptionVi; category.DescriptionEn = normalized.DescriptionEn;
        category.SortOrder = normalized.SortOrder;
        await repository.UpdateAsync(category, cancellationToken);
        return Ok(category);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var category = await repository.GetAsync(id, cancellationToken);
        if (category is null) return NotFound();
        if ((await repository.ListAsync(cancellationToken)).Count(item => item.Kind == category.Kind && item.Framework == category.Framework) == 1)
            return Conflict("At least one category must remain in each group.");
        if (await repository.IsUsedAsync(category, cancellationToken))
            return Conflict("This category is used by a course and cannot be deleted.");
        await repository.DeleteAsync(category, cancellationToken);
        return NoContent();
    }

    private static bool TryNormalize(CourseCategoryRequest request, out CourseCategoryRequest value, out string error)
    {
        value = request with {
            Kind = (request.Kind ?? "").Trim().ToLowerInvariant(), Framework = (request.Framework ?? "").Trim().ToLowerInvariant(),
            Code = (request.Code ?? "").Trim(), TitleVi = (request.TitleVi ?? "").Trim(), TitleEn = (request.TitleEn ?? "").Trim(),
            DescriptionVi = (request.DescriptionVi ?? "").Trim(), DescriptionEn = (request.DescriptionEn ?? "").Trim() };
        error = "";
        if (value.Kind is not ("domain" or "level") ||
            value.Kind == "domain" && value.Framework != "" ||
            value.Kind == "level" && value.Framework is not ("cefr" or "jlpt" or "hsk" or "topik") ||
            value.Code.Length is < 1 or > 50 || value.Kind == "level" && value.Code.Length > 20 || value.TitleVi.Length is < 1 or > 150 || value.TitleEn.Length is < 1 or > 150 ||
            value.DescriptionVi.Length > 500 || value.DescriptionEn.Length > 500 ||
            value.Code.Any(c => !(char.IsLetterOrDigit(c) || c is ' ' or '-' or '_')))
        {
            error = "Invalid category fields."; return false;
        }
        if (value.Kind == "domain") value = value with { Code = value.Code.ToLowerInvariant() };
        else value = value with { Code = value.Code.ToUpperInvariant() };
        return true;
    }
}
