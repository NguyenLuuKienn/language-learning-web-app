namespace LanguageLearning.Application.DTOs.Progress;

public class WritingSubmissionDto
{
    public Guid Id { get; set; }
    public string PromptId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SaveWritingSubmissionRequest
{
    public Guid? ClientSubmissionId { get; set; }
    public string PromptId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
