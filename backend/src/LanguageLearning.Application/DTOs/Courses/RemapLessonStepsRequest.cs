namespace LanguageLearning.Application.DTOs.Courses;

public sealed class RemapLessonStepsRequest
{
    public string ExpectedContentVersion { get; set; } = string.Empty;
    public Dictionary<string, string> StepIds { get; set; } = new();
}
