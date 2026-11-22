namespace LanguageLearning.Application.DTOs.Quiz;

public class QuizAttemptHistoryResponse
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public DateTime CompletedAt { get; set; }
}
