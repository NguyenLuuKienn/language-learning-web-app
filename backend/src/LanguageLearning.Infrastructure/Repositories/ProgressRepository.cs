using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.DTOs.Statistics;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class ProgressRepository : IProgressRepository
{
    private readonly AppDbContext _context;

    public ProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaderboardEntryResponse>> GetWeeklyLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        var activity = await _context.DailyLearningStats
            .AsNoTracking()
            .Where(item => item.LocalDate >= weekStart && item.LocalDate <= today)
            .GroupBy(item => item.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Reviews = group.Sum(item => item.ReviewCount),
                Lessons = group.Sum(item => item.LessonsCompleted)
            })
            .ToListAsync(cancellationToken);

        var userIds = activity.Select(item => item.UserId).ToList();
        var users = await _context.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id) && user.IsActive)
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return activity.Where(item => users.ContainsKey(item.UserId))
            .Select(item => new LeaderboardEntryResponse
            {
                UserId = item.UserId,
                DisplayName = users[item.UserId].DisplayName,
                ReviewCount = item.Reviews,
                LessonsCompleted = item.Lessons,
                Xp = item.Reviews * 10 + item.Lessons * 30
            })
            .OrderByDescending(item => item.Xp)
            .ThenBy(item => item.DisplayName)
            .Take(50)
            .ToList();
    }

    public async Task<LessonProgress?> GetProgressAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        return await _context.LessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, cancellationToken);
    }

    public async Task UpsertProgressAsync(LessonProgress progress, CancellationToken cancellationToken = default)
    {
        var existing = await _context.LessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == progress.UserId && p.LessonId == progress.LessonId, cancellationToken);

        if (existing == null)
        {
            await _context.LessonProgresses.AddAsync(progress, cancellationToken);
        }
        else
        {
            existing.Status = progress.Status;
            existing.CompletedAt = progress.CompletedAt;
            existing.LastAccessedAt = progress.LastAccessedAt;
            existing.CurrentStepId = progress.CurrentStepId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountCompletedLessonsAsync(Guid userId, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null, IReadOnlyCollection<string>? domains = null)
    {
        var query = FilterTarget(_context.LessonProgresses.AsNoTracking().Where(progress => progress.UserId == userId && progress.Status == "Completed"), languageId, level, domains);
        return query.CountAsync(cancellationToken);
    }

    public async Task<LessonProgress?> GetLastAccessedProgressAsync(Guid userId, CancellationToken cancellationToken = default, Guid? languageId = null, string? level = null, IReadOnlyCollection<string>? domains = null)
    {
        return await FilterTarget(_context.LessonProgresses
            .Include(p => p.Lesson)
                .ThenInclude(l => l.Unit)
                    .ThenInclude(u => u.Course)
            .Where(p => p.UserId == userId && p.Status == "InProgress"), languageId, level, domains)
            .OrderByDescending(p => p.LastAccessedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LearningRoadmapResponse> GetRoadmapAsync(Guid userId, Guid languageId, string level,
        IReadOnlyCollection<string> domains, CancellationToken cancellationToken = default)
    {
        var normalizedLevel = level.ToLowerInvariant();
        var normalizedDomains = domains.Select(domain => domain.ToLowerInvariant()).ToArray();
        var courses = await _context.Courses.AsNoTracking()
            .Where(course => course.Status == ContentStatus.Published && course.LanguageId == languageId &&
                course.Level.ToLower() == normalizedLevel && normalizedDomains.Contains(course.Domain.ToLower()))
            .OrderBy(course => course.Title)
            .Include(course => course.Units).ThenInclude(unit => unit.Lessons)
            .AsSplitQuery().ToListAsync(cancellationToken);
        var lessonIds = courses.SelectMany(course => course.Units)
            .SelectMany(unit => unit.Lessons)
            .Where(lesson => lesson.Status == ContentStatus.Published)
            .Select(lesson => lesson.Id).ToArray();
        var progresses = lessonIds.Length == 0 ? new Dictionary<Guid, LessonProgress>() :
            await _context.LessonProgresses.AsNoTracking()
                .Where(progress => progress.UserId == userId && lessonIds.Contains(progress.LessonId))
                .ToDictionaryAsync(progress => progress.LessonId, cancellationToken);

        var response = new LearningRoadmapResponse();
        foreach (var course in courses)
        {
            var priorLessonComplete = true;
            var courseResponse = new RoadmapCourseResponse
            {
                Id = course.Id, Title = course.Title, Level = course.Level, Domain = course.Domain
            };
            foreach (var unit in course.Units.OrderBy(item => item.SortOrder).ThenBy(item => item.Title))
            {
                var unitResponse = new RoadmapUnitResponse { Id = unit.Id, Title = unit.Title, SortOrder = unit.SortOrder };
                foreach (var lesson in unit.Lessons.Where(item => item.Status == ContentStatus.Published)
                    .OrderBy(item => item.SortOrder).ThenBy(item => item.Title))
                {
                    progresses.TryGetValue(lesson.Id, out var progress);
                    var status = progress?.Status ?? (priorLessonComplete ? "NotStarted" : "Locked");
                    unitResponse.Lessons.Add(new RoadmapLessonResponse
                    {
                        Id = lesson.Id, Title = lesson.Title, LessonType = lesson.LessonType,
                        SortOrder = lesson.SortOrder, Status = status,
                        LastAccessedAt = progress?.LastAccessedAt
                    });
                    courseResponse.TotalLessons++;
                    if (progress?.Status == "Completed") courseResponse.CompletedLessons++;
                    else if (status != "Locked" && response.NextLessonId == null)
                    {
                        response.NextCourseId = course.Id;
                        response.NextLessonId = lesson.Id;
                    }
                    if (progress?.Status != "Completed") priorLessonComplete = false;
                }
                if (unitResponse.Lessons.Count > 0) courseResponse.Units.Add(unitResponse);
            }
            response.TotalLessons += courseResponse.TotalLessons;
            response.CompletedLessons += courseResponse.CompletedLessons;
            response.Courses.Add(courseResponse);
        }
        return response;
    }

    private static IQueryable<LessonProgress> FilterTarget(IQueryable<LessonProgress> query, Guid? languageId, string? level, IReadOnlyCollection<string>? domains)
    {
        if (languageId.HasValue) query = query.Where(progress => progress.Lesson.Unit.Course.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level))
        {
            var normalizedLevel = level.ToLower();
            query = query.Where(progress => progress.Lesson.Unit.Course.Level.ToLower() == normalizedLevel);
        }
        if (domains != null)
        {
            var normalizedDomains = domains.Select(domain => domain.ToLower()).ToArray();
            query = query.Where(progress => normalizedDomains.Contains(progress.Lesson.Unit.Course.Domain.ToLower()));
        }
        return query;
    }

    public async Task RecordLessonCompletionActivityAsync(Guid userId, string timezone, CancellationToken cancellationToken = default)
    {
        TimeZoneInfo tzInfo;
        try
        {
            tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch
        {
            tzInfo = TimeZoneInfo.Utc;
        }

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
        var today = DateOnly.FromDateTime(localNow);

        var stats = await _context.DailyLearningStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.LocalDate == today, cancellationToken);

        if (stats == null)
        {
            stats = new DailyLearningStats
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LocalDate = today,
                Timezone = timezone,
                ReviewCount = 0,
                LessonsCompleted = 1,
                GoalMet = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.DailyLearningStats.AddAsync(stats, cancellationToken);
        }
        else
        {
            stats.LessonsCompleted += 1;
            stats.UpdatedAt = DateTime.UtcNow;
            _context.DailyLearningStats.Update(stats);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
