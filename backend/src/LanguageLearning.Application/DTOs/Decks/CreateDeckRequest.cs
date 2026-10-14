using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Decks;

public class CreateDeckRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid LanguageId { get; set; }

    public bool IsPublic { get; set; } = false;

    public List<string> Tags { get; set; } = new();
}