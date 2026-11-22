using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Cards;

public class BatchCreateCardsRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public List<CreateCardRequest> Cards { get; set; } = new();
}
