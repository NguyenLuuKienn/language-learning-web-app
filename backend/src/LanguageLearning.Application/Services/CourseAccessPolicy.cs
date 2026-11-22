using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

internal static class CourseAccessPolicy
{
    public static bool CanAccess(Course course, StudyTargetDto? target) =>
        course.Status == ContentStatus.Published && target != null
        && target.LanguageId == course.LanguageId
        && string.Equals(target.Level, course.Level, StringComparison.OrdinalIgnoreCase)
        && (target.Domains?.Count > 0 ? target.Domains : [target.Domain])
            .Any(domain => string.Equals(domain, course.Domain, StringComparison.OrdinalIgnoreCase));
}
