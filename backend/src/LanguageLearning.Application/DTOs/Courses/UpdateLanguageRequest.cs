using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Courses;

public class UpdateLanguageRequest
{
    [Required]
    [NotBlank]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string NativeName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string FlagEmoji { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
