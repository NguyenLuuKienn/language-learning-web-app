using LanguageLearning.Application.DTOs.Notebook;

namespace LanguageLearning.Application.Interfaces;

public interface INotebookService
{
    Task<(List<NotebookEntryResponse> Items, int Total)> GetPageAsync(Guid userId, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<NotebookEntryResponse> CreateAsync(Guid userId, NotebookEntryRequest request, CancellationToken cancellationToken);
    Task<NotebookEntryResponse> UpdateAsync(Guid userId, Guid entryId, NotebookEntryRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);
}
