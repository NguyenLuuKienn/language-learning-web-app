using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Notebook;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public sealed class NotebookService : INotebookService
{
    private readonly INotebookRepository _repository;
    public NotebookService(INotebookRepository repository) => _repository = repository;

    public async Task<(List<NotebookEntryResponse> Items, int Total)> GetPageAsync(
        Guid userId, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedSearch = search?.Trim();
        if (normalizedSearch?.Length > 200)
            throw new BusinessRuleException("NOTEBOOK_SEARCH_TOO_LONG", Messages.Notebook.SearchTooLong);
        var result = await _repository.GetPageAsync(userId, normalizedSearch, Math.Clamp(page, 1, 100000), Math.Clamp(pageSize, 1, 50), cancellationToken);
        return (result.Items.Select(Map).ToList(), result.Total);
    }

    public async Task<NotebookEntryResponse> CreateAsync(Guid userId, NotebookEntryRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var entry = new NotebookEntry { UserId = userId };
        Apply(entry, request);
        await _repository.SaveAsync(entry, cancellationToken);
        return Map(entry);
    }

    public async Task<NotebookEntryResponse> UpdateAsync(Guid userId, Guid entryId, NotebookEntryRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var entry = await _repository.GetByIdAsync(userId, entryId, cancellationToken)
            ?? throw new NotFoundException(Messages.Notebook.NotFound);
        Apply(entry, request);
        await _repository.SaveAsync(entry, cancellationToken);
        return Map(entry);
    }

    public async Task DeleteAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(userId, entryId, cancellationToken)
            ?? throw new NotFoundException(Messages.Notebook.NotFound);
        await _repository.DeleteAsync(entry, cancellationToken);
    }

    private static void Validate(NotebookEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text) && string.IsNullOrWhiteSpace(request.Note))
            throw new BusinessRuleException("NOTEBOOK_EMPTY", Messages.Notebook.Empty);
        if (request.Text?.Length > 500 || request.Note?.Length > 5000 || request.LanguageCode?.Length > 12 ||
            request.SourcePath?.Length > 500 || request.Context?.Length > 1000)
            throw new BusinessRuleException("NOTEBOOK_TOO_LONG", Messages.Notebook.TooLong);
    }

    private static void Apply(NotebookEntry entry, NotebookEntryRequest request)
    {
        entry.Text = request.Text?.Trim() ?? string.Empty;
        entry.Note = request.Note?.Trim() ?? string.Empty;
        entry.LanguageCode = request.LanguageCode?.Trim().ToLowerInvariant() ?? string.Empty;
        entry.SourcePath = request.SourcePath?.Trim() ?? string.Empty;
        entry.Context = request.Context?.Trim() ?? string.Empty;
    }

    private static NotebookEntryResponse Map(NotebookEntry entry) => new()
    {
        Id = entry.Id, Text = entry.Text, Note = entry.Note, LanguageCode = entry.LanguageCode,
        SourcePath = entry.SourcePath, Context = entry.Context, CreatedAt = entry.CreatedAt, UpdatedAt = entry.UpdatedAt
    };
}
