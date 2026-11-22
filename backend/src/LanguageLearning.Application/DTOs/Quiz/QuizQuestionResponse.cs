using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.DTOs.Quiz;

public class QuizQuestionResponse
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public QuestionType Type { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}";
    public List<string> MatchingTerms { get; set; } = new();
    public List<string> MatchingChoices { get; set; } = new();
    public List<QuestionOptionResponse> Options { get; set; } = new();
}
