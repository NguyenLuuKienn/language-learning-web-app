using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Auth;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(15)]
    [MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}
