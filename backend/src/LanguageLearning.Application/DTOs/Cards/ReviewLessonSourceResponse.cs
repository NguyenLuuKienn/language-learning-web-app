namespace LanguageLearning.Application.DTOs.Cards;

public sealed class ReviewLessonSourceResponse
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public int CardCount { get; set; }
}
