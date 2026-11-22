using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Notebook;

public sealed class NotebookEntryRequest
{
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;
    [MaxLength(5000)]
    public string Note { get; set; } = string.Empty;
    [MaxLength(12)]
    public string LanguageCode { get; set; } = string.Empty;
    [MaxLength(500)]
    public string SourcePath { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string Context { get; set; } = string.Empty;
}
