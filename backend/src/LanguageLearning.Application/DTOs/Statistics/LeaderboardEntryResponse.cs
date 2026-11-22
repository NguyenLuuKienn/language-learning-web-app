namespace LanguageLearning.Application.DTOs.Statistics;

public class LeaderboardEntryResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int ReviewCount { get; set; }
    public int LessonsCompleted { get; set; }
    public int Xp { get; set; }
    public int CurrentStreak { get; set; }
}
