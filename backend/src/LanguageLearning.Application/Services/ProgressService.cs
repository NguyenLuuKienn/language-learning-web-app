using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public class ProgressService : IProgressService
{
    private readonly IProgressRepository _progressRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;
    private readonly ILessonJourneyRepository _journeyRepository;

    public ProgressService(
        IProgressRepository progressRepository,
        ICourseRepository courseRepository,
        IStudyAccessService access,
        ICurrentUserService currentUser,
        ILessonJourneyRepository journeyRepository)
    {
        _progressRepository = progressRepository;
        _courseRepository = courseRepository;
        _access = access;
        _currentUser = currentUser;
        _journeyRepository = journeyRepository;
    }

    public async Task<LessonProgressResponse> GetLessonProgressAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin() && !await _access.CanAccessLessonAsync(userId, lessonId, cancellationToken))
            throw new NotFoundException(Messages.Course.LessonNotFound);
        var progress = await _progressRepository.GetProgressAsync(userId, lessonId, cancellationToken);
        if (progress == null)
        {
            return new LessonProgressResponse
            {
                LessonId = lessonId,
                Status = "NotStarted",
                CompletedAt = null,
                LastAccessedAt = DateTime.UtcNow
            };
        }

        return new LessonProgressResponse
        {
            LessonId = progress.LessonId,
            Status = progress.Status,
            CompletedAt = progress.CompletedAt,
            LastAccessedAt = progress.LastAccessedAt
        };
    }

    public async Task<LessonProgressResponse> UpdateLessonProgressAsync(Guid userId, Guid lessonId, string status, CancellationToken cancellationToken = default)
    {
        if (status is not ("NotStarted" or "InProgress" or "Completed"))
            throw new ArgumentException(Messages.Course.InvalidProgressStatus);
        if (status == "Completed")
            throw new BusinessRuleException("LESSON_COMPLETION_REQUIRES_EVIDENCE", Messages.Course.CompletionRequiresAssessment);
        if (!_currentUser.IsAdmin() && !await _access.CanAccessLessonAsync(userId, lessonId, cancellationToken))
            throw new NotFoundException(Messages.Course.LessonNotFound);
        var lesson = await _courseRepository.GetLessonByIdAsync(lessonId, false, cancellationToken);
        if (lesson == null)
        {
            throw new NotFoundException(Messages.Course.LessonNotFound);
        }

        var progress = await _progressRepository.GetProgressAsync(userId, lessonId, cancellationToken);
        if (progress?.Status == "Completed")
            return new LessonProgressResponse
            {
                LessonId = lessonId, Status = "Completed", CompletedAt = progress.CompletedAt,
                LastAccessedAt = progress.LastAccessedAt
            };
        if (progress == null)
        {
            progress = new LessonProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LessonId = lessonId,
                Status = status,
                CompletedAt = null,
                LastAccessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            progress.Status = status;
            progress.CompletedAt = null;
            progress.LastAccessedAt = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;
        }

        await _progressRepository.UpsertProgressAsync(progress, cancellationToken);

        return new LessonProgressResponse
        {
            LessonId = progress.LessonId,
            Status = progress.Status,
            CompletedAt = progress.CompletedAt,
            LastAccessedAt = progress.LastAccessedAt
        };
    }

    public async Task<CourseProgressResponse> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin() && !await _access.CanAccessCourseAsync(userId, courseId, cancellationToken))
            throw new NotFoundException(Messages.Course.CourseNotFound);
        var course = await _courseRepository.GetCourseByIdAsync(courseId, true, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException(Messages.Course.CourseNotFound);
        }

        var allLessons = course.Units.SelectMany(u => u.Lessons).ToList();
        int totalLessons = allLessons.Count;

        var userProgresses = await _courseRepository.GetUserLessonProgressesAsync(userId, courseId, cancellationToken);
        int completedLessons = userProgresses.Count(p => p.Status == "Completed");

        return new CourseProgressResponse
        {
            CourseId = courseId,
            TotalLessons = totalLessons,
            CompletedLessons = completedLessons
        };
    }

    public async Task<ResumeLearningResponse?> GetResumeLearningAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var target = _currentUser.IsAdmin() ? null : await _access.GetTargetAsync(userId, cancellationToken);
        if (!_currentUser.IsAdmin() && target == null) return null;
        var domains = target?.Domains?.Count > 0 ? target.Domains : target == null ? null : [target.Domain];
        var last = await _progressRepository.GetLastAccessedProgressAsync(userId, cancellationToken, target?.LanguageId, target?.Level, domains);
        if (last is { Status: "InProgress", Lesson: not null } &&
            (_currentUser.IsAdmin() || await _access.CanAccessLessonAsync(userId, last.LessonId, cancellationToken)))
        {
            var step = (await _journeyRepository.GetStepsAsync(userId, last.LessonId, cancellationToken))
                .FirstOrDefault(item => item.StepId == last.CurrentStepId);
            return new ResumeLearningResponse
            {
                CourseId = last.Lesson.Unit?.CourseId ?? Guid.Empty,
                CourseTitle = last.Lesson.Unit?.Course?.Title ?? string.Empty,
                LessonId = last.LessonId,
                LessonTitle = last.Lesson.Title,
                LastAccessedAt = last.LastAccessedAt,
                StepId = last.CurrentStepId,
                ActivityId = step?.ActivityId,
                Cursor = step?.Cursor ?? 0
            };
        }
        if (target == null) return null;
        var roadmap = await _progressRepository.GetRoadmapAsync(userId, target.LanguageId, target.Level,
            domains ?? [target.Domain], cancellationToken);
        if (roadmap.NextLessonId == null) return null;
        var course = roadmap.Courses.First(item => item.Id == roadmap.NextCourseId);
        var lesson = course.Units.SelectMany(item => item.Lessons).First(item => item.Id == roadmap.NextLessonId);
        return new ResumeLearningResponse
        {
            CourseId = course.Id, CourseTitle = course.Title, LessonId = lesson.Id,
            LessonTitle = lesson.Title, LastAccessedAt = lesson.LastAccessedAt ?? DateTime.UtcNow
        };
    }

    public async Task<LearningRoadmapResponse> GetRoadmapAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var target = await _access.GetTargetAsync(userId, cancellationToken);
        if (target == null) return new LearningRoadmapResponse();
        var domains = target.Domains.Count > 0 ? target.Domains : [target.Domain];
        return await _progressRepository.GetRoadmapAsync(userId, target.LanguageId, target.Level, domains, cancellationToken);
    }
}
