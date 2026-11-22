namespace LanguageLearning.Application.DTOs.Quiz;

public class QuestionOptionResponse
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
