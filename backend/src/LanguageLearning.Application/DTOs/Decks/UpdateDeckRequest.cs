using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Decks;

public class UpdateDeckRequest
{
    [StringLength(200, MinimumLength = 1)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public Guid? LanguageId { get; set; }

    public bool? IsPublic { get; set; }

    public List<string>? Tags { get; set; }
}