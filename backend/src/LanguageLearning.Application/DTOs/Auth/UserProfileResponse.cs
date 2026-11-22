using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.DTOs.Auth;

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public string? AvatarUrl { get; set; }
    public int DailyTarget { get; set; } = 10;
    public int CurrentStreak { get; set; } = 0;
}
