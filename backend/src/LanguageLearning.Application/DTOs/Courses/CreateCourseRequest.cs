using System.ComponentModel.DataAnnotations;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Courses;

public class CreateCourseRequest
{
    [Required]
    [NotEmptyGuid]
    public Guid LanguageId { get; set; }

    [Required]
    [NotBlank]
    [MinLength(2)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [NotBlank]
    [MaxLength(20)]
    public string Level { get; set; } = "A1";

    [Required]
    [NotBlank]
    [MaxLength(50)]
    public string Domain { get; set; } = "General";

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    public ContentStatus Status { get; set; } = ContentStatus.Published;
}
