using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Quiz;

public class QuizAnswerSubmission
{
    [Required]
    [NotEmptyGuid]
    public Guid QuestionId { get; set; }

    public Guid? SelectedOptionId { get; set; }

    public string? TextAnswer { get; set; }

    public string? MatchingJson { get; set; }
}
