namespace LanguageLearning.Application.DTOs.Auth;

public sealed class PlacementQuestionDto
{
    public string Id { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? AudioScript { get; set; }
    public string? ReadingPassage { get; set; }
    public List<string>? Options { get; set; }
    public int? MinWords { get; set; }
    public int Points { get; set; }
}

public sealed class PlacementExamDto
{
    public Guid CourseId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public List<PlacementQuestionDto> Questions { get; set; } = [];
}

public sealed class PlacementSubmissionDto
{
    public Dictionary<string, int> SelectedOptions { get; set; } = [];
    public Dictionary<string, string> WrittenAnswers { get; set; } = [];
    public Dictionary<string, string> AudioFileNames { get; set; } = [];
}

public sealed class PlacementReviewDto
{
    public Guid AttemptId { get; set; }
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public string LearnerName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public List<PlacementReviewQuestionDto> Questions { get; set; } = [];
}

public sealed class PlacementReviewQuestionDto
{
    public string Id { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? Rubric { get; set; }
    public string? ExpectedResponse { get; set; }
    public string? WrittenAnswer { get; set; }
    public string? AudioUrl { get; set; }
    public int Points { get; set; }
}

public sealed class PlacementReviewRequestDto
{
    public Dictionary<string, int> Scores { get; set; } = [];
    public string Feedback { get; set; } = string.Empty;
}

public sealed class PlacementSectionScoreDto
{
    public int Earned { get; set; }
    public int Total { get; set; }
}

public sealed class PlacementResultDto
{
    public Guid AttemptId { get; set; }
    public string Status { get; set; } = "Pending";
    public string Feedback { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string TargetLevel { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int EarnedPoints { get; set; }
    public int Percentage { get; set; }
    public bool Passed { get; set; }
    public Dictionary<string, PlacementSectionScoreDto> SectionBreakdown { get; set; } = [];
    public DateTime AttemptedAt { get; set; }
    public string? PlacementProof { get; set; }
}
