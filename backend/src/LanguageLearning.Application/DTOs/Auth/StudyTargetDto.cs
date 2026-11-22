namespace LanguageLearning.Application.DTOs.Auth;

public class StudyTargetDto
{
    public Guid LanguageId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public string? NativeName { get; set; }
    public string? FlagEmoji { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public List<string> Domains { get; set; } = new();
    public string? PlacementProof { get; set; }
}
