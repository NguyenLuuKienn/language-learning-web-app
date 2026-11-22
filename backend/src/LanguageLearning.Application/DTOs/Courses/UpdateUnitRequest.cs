using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Courses;

public class UpdateUnitRequest
{
    [Required]
    [NotBlank]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }
}
