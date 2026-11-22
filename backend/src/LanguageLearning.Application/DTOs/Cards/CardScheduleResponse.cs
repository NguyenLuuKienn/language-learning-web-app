namespace LanguageLearning.Application.DTOs.Cards;

public class CardScheduleResponse
{
    public Guid CardId { get; set; }
    public DateTime DueAt { get; set; }
    public double IntervalDays { get; set; }
    public double EaseFactor { get; set; }
    public int Repetitions { get; set; }
    public int Lapses { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public bool IsDue => DueAt <= DateTime.UtcNow;
}
