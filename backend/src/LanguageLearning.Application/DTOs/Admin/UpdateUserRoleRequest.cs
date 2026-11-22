using System.ComponentModel.DataAnnotations;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.DTOs.Admin;

public class UpdateUserRoleRequest
{
    [Required]
    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }
}
