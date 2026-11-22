namespace LanguageLearning.Application.DTOs.Quiz;

public class QuizResultResponse
{
    public Guid AttemptId { get; set; }
    public Guid LessonId { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public double Percentage => MaxScore > 0 ? Math.Round((double)Score / MaxScore * 100, 1) : 0;
    public bool Passed => Percentage >= 70.0;
    public DateTime CompletedAt { get; set; }
    public List<QuestionFeedbackResponse> Details { get; set; } = new();
}

public class QuestionFeedbackResponse
{
    public Guid QuestionId { get; set; }
    public bool IsCorrect { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
