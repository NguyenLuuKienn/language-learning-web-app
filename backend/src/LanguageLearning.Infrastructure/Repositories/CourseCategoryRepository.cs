using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public sealed class CourseCategoryRepository(AppDbContext db) : ICourseCategoryRepository
{
    public Task<List<CourseCategory>> ListAsync(CancellationToken cancellationToken = default) =>
        db.CourseCategories.AsNoTracking().OrderBy(item => item.Kind).ThenBy(item => item.Framework).ThenBy(item => item.SortOrder).ThenBy(item => item.Code).ToListAsync(cancellationToken);

    public Task<CourseCategory?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.CourseCategories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(string kind, string framework, string code, CancellationToken cancellationToken = default) =>
        db.CourseCategories.AnyAsync(item => item.Kind == kind && item.Framework == framework && item.Code == code, cancellationToken);

    public Task<bool> IsUsedAsync(CourseCategory category, CancellationToken cancellationToken = default) =>
        category.Kind == "domain"
            ? db.Courses.AnyAsync(course => course.Domain == category.Code, cancellationToken)
            : db.Courses.AnyAsync(course => course.Level == category.Code &&
                (category.Framework == "cefr" && course.Language.Code != "ja" && course.Language.Code != "jp" && course.Language.Code != "zh" && course.Language.Code != "cn" && course.Language.Code != "ko" && course.Language.Code != "kr" ||
                 category.Framework == "jlpt" && (course.Language.Code == "ja" || course.Language.Code == "jp") ||
                 category.Framework == "hsk" && (course.Language.Code == "zh" || course.Language.Code == "cn") ||
                 category.Framework == "topik" && (course.Language.Code == "ko" || course.Language.Code == "kr")), cancellationToken);

    public async Task AddAsync(CourseCategory category, CancellationToken cancellationToken = default)
    {
        db.CourseCategories.Add(category); await db.SaveChangesAsync(cancellationToken);
    }
    public async Task UpdateAsync(CourseCategory category, CancellationToken cancellationToken = default)
    {
        category.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(cancellationToken);
    }
    public async Task DeleteAsync(CourseCategory category, CancellationToken cancellationToken = default)
    {
        db.CourseCategories.Remove(category); await db.SaveChangesAsync(cancellationToken);
    }
}
