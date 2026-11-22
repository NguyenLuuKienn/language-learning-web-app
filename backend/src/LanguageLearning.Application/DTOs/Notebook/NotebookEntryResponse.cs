namespace LanguageLearning.Application.DTOs.Notebook;

public sealed class NotebookEntryResponse
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public string LanguageCode { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
