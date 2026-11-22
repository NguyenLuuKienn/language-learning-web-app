using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Cards;

public class UpdateCardRequest
{
    [Required]
    [NotBlank]
    [MaxLength(1000)]
    public string Front { get; set; } = string.Empty;

    [Required]
    [NotBlank]
    [MaxLength(4000)]
    public string Back { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Phonetic { get; set; }
    [MaxLength(60)]
    public string? PartOfSpeech { get; set; }
    public List<string>? Meanings { get; set; }
    [MaxLength(2000)]
    public string? Example { get; set; }

    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
}
