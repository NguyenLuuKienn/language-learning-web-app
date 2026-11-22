namespace LanguageLearning.Application.DTOs.Progress;

public sealed class SkillPracticeRequest
{
    public Guid CourseId { get; set; }
    public string ActivityId { get; set; } = string.Empty;
    public int? AnswerIndex { get; set; }
    public bool Practiced { get; set; }
    public Guid? ClientAttemptId { get; set; }
}

public sealed class SkillPracticeResponse
{
    public Guid CourseId { get; init; }
    public string ActivityId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Attempts { get; init; }
    public int CorrectAttempts { get; init; }
    public bool? Correct { get; init; }
    public DateTime UpdatedAt { get; init; }
}
