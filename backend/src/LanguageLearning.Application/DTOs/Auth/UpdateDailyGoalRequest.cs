using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.DTOs.Auth;

public class UpdateDailyGoalRequest
{
    [Range(1, 100)]
    public int DailyTarget { get; set; } = 10;
}
