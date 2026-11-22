using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Admin;

public class UpdateUserStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
