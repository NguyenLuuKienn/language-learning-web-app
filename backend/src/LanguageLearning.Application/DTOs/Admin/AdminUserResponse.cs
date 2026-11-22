using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.DTOs.Admin;

public class AdminUserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
