using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Progress;

public class UpdateProgressRequest
{
    [Required]
    [RegularExpression("^(NotStarted|InProgress|Completed)$")]
    public string Status { get; set; } = "InProgress";
}
