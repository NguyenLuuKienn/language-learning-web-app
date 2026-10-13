using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Learner;
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Deck> Decks { get; set; } = new List<Deck>();
    public ICollection<CardSchedule> CardSchedules { get; set; } = new List<CardSchedule>();
    public ICollection<ReviewEvent> ReviewEvents { get; set; } = new List<ReviewEvent>();
    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<DailyLearningStats> DailyLearningStats { get; set; } = new List<DailyLearningStats>();
    public ICollection<UserGoal> UserGoals { get; set; } = new List<UserGoal>();
    public ICollection<SyncOperation> SyncOperations { get; set; } = new List<SyncOperation>();
}

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked => RevokedAt != null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
