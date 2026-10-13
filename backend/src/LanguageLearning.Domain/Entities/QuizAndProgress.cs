using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Domain.Entities;

public class Question : BaseEntity
{
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public string Prompt { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}"; // Dùng cho matching hoặc fill-in-blank

    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
}

public class QuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}

public class QuizAttempt : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}

public class QuizAnswer : BaseEntity
{
    public Guid AttemptId { get; set; }
    public QuizAttempt Attempt { get; set; } = null!;
    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public string AnswerJson { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class LessonProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public string Status { get; set; } = "InProgress"; // NotStarted, InProgress, Completed
    public DateTime? CompletedAt { get; set; }
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
}

public class CourseEnrollment : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}

public class DailyLearningStats : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly LocalDate { get; set; }
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public int ReviewCount { get; set; }
    public int LessonsCompleted { get; set; }
    public bool GoalMet { get; set; }
}

public class UserGoal : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int DailyTarget { get; set; } = 10; // 10 cards per day
    public string GoalType { get; set; } = "CardsReview";
}

public class SyncOperation : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ClientOperationId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Accepted"; // Accepted, Duplicate, Conflict, Rejected
}

public class AuditLog : BaseEntity
{
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
