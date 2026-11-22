using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.DTOs.Auth;

namespace LanguageLearning.Application.Interfaces;

public interface ICourseService
{
    Task<List<CourseResponse>> GetCoursesAsync(Guid? languageId, string? level, string? domain = null, Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<CourseDetailResponse> GetCourseDetailAsync(Guid courseId, Guid? currentUserId = null, CancellationToken cancellationToken = default, StudyTargetDto? target = null, bool isAdmin = false);
    Task EnrollCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default, StudyTargetDto? target = null, bool isAdmin = false);

    Task<CurriculumExportResponse> ExportCurriculumAsync(Guid? languageId = null, CancellationToken cancellationToken = default);

    Task<LessonDetailResponse> GetLessonDetailAsync(Guid lessonId, Guid? currentUserId = null, CancellationToken cancellationToken = default, StudyTargetDto? target = null, bool isAdmin = false);

    Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken = default);
    Task<CourseResponse> UpdateCourseAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken = default);
    Task DeleteCourseAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UnitResponse> CreateUnitAsync(Guid courseId, CreateUnitRequest request, CancellationToken cancellationToken = default);
    Task<UnitResponse> UpdateUnitAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default);
    Task DeleteUnitAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LessonDetailResponse> CreateLessonAsync(Guid unitId, CreateLessonRequest request, CancellationToken cancellationToken = default);
    Task<LessonDetailResponse> UpdateLessonAsync(Guid id, UpdateLessonRequest request, CancellationToken cancellationToken = default);
    Task DeleteLessonAsync(Guid id, CancellationToken cancellationToken = default);
}
