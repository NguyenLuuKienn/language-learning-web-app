using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface ICourseCategoryRepository
{
    Task<List<CourseCategory>> ListAsync(CancellationToken cancellationToken = default);
    Task<CourseCategory?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string kind, string framework, string code, CancellationToken cancellationToken = default);
    Task<bool> IsUsedAsync(CourseCategory category, CancellationToken cancellationToken = default);
    Task AddAsync(CourseCategory category, CancellationToken cancellationToken = default);
    Task UpdateAsync(CourseCategory category, CancellationToken cancellationToken = default);
    Task DeleteAsync(CourseCategory category, CancellationToken cancellationToken = default);
}
