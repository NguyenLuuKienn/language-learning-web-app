using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface INotebookRepository
{
    Task<(List<NotebookEntry> Items, int Total)> GetPageAsync(Guid userId, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<NotebookEntry?> GetByIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);
    Task SaveAsync(NotebookEntry entry, CancellationToken cancellationToken);
    Task DeleteAsync(NotebookEntry entry, CancellationToken cancellationToken);
}
