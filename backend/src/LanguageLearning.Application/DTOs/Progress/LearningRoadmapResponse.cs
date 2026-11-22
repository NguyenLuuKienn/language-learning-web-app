namespace LanguageLearning.Application.DTOs.Progress;

public sealed class LearningRoadmapResponse
{
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public Guid? NextCourseId { get; set; }
    public Guid? NextLessonId { get; set; }
    public List<RoadmapCourseResponse> Courses { get; set; } = [];
}

public sealed class RoadmapCourseResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public List<RoadmapUnitResponse> Units { get; set; } = [];
}

public sealed class RoadmapUnitResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<RoadmapLessonResponse> Lessons { get; set; } = [];
}

public sealed class RoadmapLessonResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Status { get; set; } = "NotStarted";
    public DateTime? LastAccessedAt { get; set; }
}
