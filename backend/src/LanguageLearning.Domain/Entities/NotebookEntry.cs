using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public sealed class NotebookEntry : BaseEntity
{
    public Guid UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
}
