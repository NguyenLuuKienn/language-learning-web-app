using System.Text.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.DTOs.Courses;

namespace LanguageLearning.Application.Interfaces;

public interface ICourseMaterialService
{
    Task<JsonElement?> GetCourseMaterialAsync(Guid courseId, StudyTargetDto? target, bool isAdmin, CancellationToken cancellationToken = default);
    Task ImportCourseMaterialAsync(Guid courseId, Guid adminId, JsonElement material, CancellationToken cancellationToken = default);
    Task<JsonElement> GetPlacementTestsAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task UpdatePlacementTestsAsync(Guid courseId, JsonElement tests, CancellationToken cancellationToken = default);
    Task UpdateLessonMaterialAsync(Guid courseId, Guid lessonId, JsonElement lesson, CancellationToken cancellationToken = default);
    Task RemapLessonStepsAsync(Guid courseId, Guid lessonId, RemapLessonStepsRequest request, CancellationToken cancellationToken = default);
}
