using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public sealed class PlacementReviewRepository : IPlacementReviewRepository
{
    private readonly AppDbContext _db;

    public PlacementReviewRepository(AppDbContext db) => _db = db;

    public async Task AddAttemptAsync(PlacementAttempt attempt, CancellationToken cancellationToken = default)
    {
        _db.PlacementAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<PlacementAttempt?> GetAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default) =>
        _db.PlacementAttempts.FirstOrDefaultAsync(attempt => attempt.Id == attemptId, cancellationToken);

    public Task<PlacementAttempt?> GetLatestAttemptAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default) =>
        _db.PlacementAttempts.AsNoTracking().Where(attempt => attempt.UserId == userId && attempt.CourseId == courseId)
            .OrderByDescending(attempt => attempt.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public Task<List<PlacementAttempt>> GetPendingAsync(CancellationToken cancellationToken = default) =>
        _db.PlacementAttempts.AsNoTracking().Where(attempt => attempt.Status == "Pending")
            .OrderBy(attempt => attempt.CreatedAt).Take(100).ToListAsync(cancellationToken);

    public async Task<(Dictionary<Guid, string> Learners, Dictionary<Guid, string> Courses)> GetPendingLabelsAsync(
        IReadOnlyCollection<Guid> userIds, IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default)
    {
        var learners = await _db.Users.AsNoTracking().Where(user => userIds.Contains(user.Id))
            .Select(user => new { user.Id, user.DisplayName }).ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        var courses = await _db.Courses.AsNoTracking().Where(course => courseIds.Contains(course.Id))
            .Select(course => new { course.Id, course.Title }).ToDictionaryAsync(item => item.Id, item => item.Title, cancellationToken);
        return (learners, courses);
    }

    public Task SaveAttemptAsync(CancellationToken cancellationToken = default) => _db.SaveChangesAsync(cancellationToken);

    public async Task AddAudioAsync(PlacementAudio audio, CancellationToken cancellationToken = default)
    {
        _db.PlacementAudios.Add(audio);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> OwnsAudioAsync(Guid userId, Guid courseId, string questionId, string fileName, CancellationToken cancellationToken = default) =>
        _db.PlacementAudios.AsNoTracking().AnyAsync(audio => audio.UserId == userId && audio.CourseId == courseId &&
            audio.QuestionId == questionId && audio.FileName == fileName, cancellationToken);

    public Task<bool> OwnsFileAsync(Guid userId, string fileName, CancellationToken cancellationToken = default) =>
        _db.PlacementAudios.AsNoTracking().AnyAsync(audio => audio.UserId == userId && audio.FileName == fileName, cancellationToken);
}
