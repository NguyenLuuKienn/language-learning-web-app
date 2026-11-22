namespace LanguageLearning.Application.DTOs.Courses;

public class LanguageResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
