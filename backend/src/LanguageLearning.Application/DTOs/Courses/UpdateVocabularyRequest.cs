using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Courses;

public class UpdateVocabularyRequest
{
    [Required]
    [NotBlank]
    [MaxLength(100)]
    public string Term { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Phonetic { get; set; } = string.Empty;

    [Required]
    [NotBlank]
    [MaxLength(300)]
    public string Meaning { get; set; } = string.Empty;
    public List<string>? Meanings { get; set; }
    [MaxLength(60)]
    public string? PartOfSpeech { get; set; }

    [MaxLength(2000)]
    public string Example { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
}
