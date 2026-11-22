using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Quiz;

public class SubmitQuizRequest
{
    [Required]
    [NotEmptyGuid]
    public Guid LessonId { get; set; }
    public Guid? ClientAttemptId { get; set; }

    public List<QuizAnswerSubmission> Answers { get; set; } = new();
}
