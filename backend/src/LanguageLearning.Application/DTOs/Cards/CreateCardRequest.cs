
using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Cards;

public class CreateCardRequest
{
    [Required]
    public string Front { get; set; } = string.Empty;

    [Required]
    public string Back { get; set; } = string.Empty;
}
