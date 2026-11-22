using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public class WritingSubmission : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string PromptId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Guid? ClientSubmissionId { get; set; }
}
