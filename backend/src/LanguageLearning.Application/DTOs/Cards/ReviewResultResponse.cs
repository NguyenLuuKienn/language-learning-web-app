using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.DTOs.Cards;

public class ReviewResultResponse
{
    public Guid EventId { get; set; }
    public Guid ClientEventId { get; set; }
    public Guid CardId { get; set; }
    public SrsRating Rating { get; set; }
    public DateTime DueAt { get; set; }
    public double NextIntervalDays { get; set; }
    public bool IsDuplicate { get; set; }
}
