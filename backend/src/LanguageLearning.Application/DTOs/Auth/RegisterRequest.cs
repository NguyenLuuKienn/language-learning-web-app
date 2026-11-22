using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(15)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@".*\S.*", ErrorMessage = "Display name cannot be blank.")]
    [MinLength(2)]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Timezone { get; set; } = "Asia/Ho_Chi_Minh";
}
