using LanguageLearning.Application.DTOs.Progress;

namespace LanguageLearning.Application.Interfaces;

public interface IProgressService
{
    Task<LessonProgressResponse> GetLessonProgressAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default);
    Task<LessonProgressResponse> UpdateLessonProgressAsync(Guid userId, Guid lessonId, string status, CancellationToken cancellationToken = default);
    Task<CourseProgressResponse> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);
    Task<ResumeLearningResponse?> GetResumeLearningAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<LearningRoadmapResponse> GetRoadmapAsync(Guid userId, CancellationToken cancellationToken = default);
}
