using System.ComponentModel.DataAnnotations;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Courses;

public class UpdateLessonRequest
{
    [Required]
    [NotBlank]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(20000)]
    public string Content { get; set; } = string.Empty;
    [Required]
    [RegularExpression("^(Vocabulary|Grammar|Listening|Reading|Writing)$")]
    public string LessonType { get; set; } = "Vocabulary";
    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Published;
}
