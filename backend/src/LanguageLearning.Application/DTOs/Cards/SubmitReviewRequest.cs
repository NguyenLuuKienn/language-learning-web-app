using System.ComponentModel.DataAnnotations;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Cards;

public class SubmitReviewRequest
{
    [Required]
    [NotEmptyGuid]
    public Guid ClientEventId { get; set; }

    [Required]
    [NotEmptyGuid]
    public Guid CardId { get; set; }

    [Required]
    [EnumDataType(typeof(SrsRating))]
    public SrsRating Rating { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public string AlgorithmVersion { get; set; } = "srs-v1";
}
