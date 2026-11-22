namespace LanguageLearning.Application.DTOs.Progress;

public sealed class LessonJourneyResponse
{
    public Guid CourseId { get; set; }
    public Guid UnitId { get; set; }
    public Guid LessonId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = "NotStarted";
    public string ContentVersion { get; set; } = string.Empty;
    public bool ContentChangedSinceStart { get; set; }
    public string CurrentStepId { get; set; } = string.Empty;
    public int CompletedRequiredSteps { get; set; }
    public int TotalRequiredSteps { get; set; }
    public List<LessonJourneyStepResponse> Steps { get; set; } = [];
    public List<LearningWeakPointResponse> WeakPoints { get; set; } = [];
}

public sealed class LearningWeakPointResponse
{
    public string StepType { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public int IncorrectAttempts { get; set; }
}

public sealed class LessonJourneyStepResponse
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool Required { get; set; } = true;
    public string CompletionPolicy { get; set; } = string.Empty;
    public List<string> ActivityRefs { get; set; } = [];
    public string Status { get; set; } = "Available";
    public string? ActivityId { get; set; }
    public int Cursor { get; set; }
    public int Revision { get; set; }
    public string DraftJson { get; set; } = "{}";
}

public sealed class SaveLessonJourneyStepRequest
{
    public string? ContentVersion { get; set; }
    public string? ActivityId { get; set; }
    public int Cursor { get; set; }
    public int ExpectedRevision { get; set; }
    public string? DraftJson { get; set; }
    public bool CompleteIntroduction { get; set; }
}
