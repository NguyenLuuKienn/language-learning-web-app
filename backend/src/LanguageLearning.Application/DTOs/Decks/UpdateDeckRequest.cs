using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Decks;

public class UpdateDeckRequest
{
    [StringLength(200, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Deck title cannot be blank.")]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public Guid? LanguageId { get; set; }

    public bool? IsPublic { get; set; }

    public List<string>? Tags { get; set; }
}
