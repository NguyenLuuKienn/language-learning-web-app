using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Decks;

public class CreateDeckRequest
{
    [Required]
    [NotBlank]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [NotEmptyGuid]
    public Guid LanguageId { get; set; }

    public bool IsPublic { get; set; } = false;

    public List<string> Tags { get; set; } = new();
}
