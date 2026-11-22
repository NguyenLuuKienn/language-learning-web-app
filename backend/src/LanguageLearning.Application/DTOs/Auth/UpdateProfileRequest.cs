using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Auth;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    [MinLength(2)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Display name cannot be blank.")]
    public string? DisplayName { get; set; }

    [MaxLength(100)]
    public string? Timezone { get; set; }

    [MaxLength(2048)]
    [Url]
    public string? AvatarUrl { get; set; }
}
