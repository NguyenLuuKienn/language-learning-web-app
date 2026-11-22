using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<bool> RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.RefreshTokens
            .Where(item => item.Id == token.Id && item.RevokedAt == null && item.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.RevokedAt, now)
                .SetProperty(item => item.UpdatedAt, now), cancellationToken) == 1;
    }

    public async Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var t in activeTokens)
        {
            t.RevokedAt = DateTime.UtcNow;
            t.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserGoal?> GetUserGoalAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGoals
            .FirstOrDefaultAsync(g => g.UserId == userId, cancellationToken);
    }

    public async Task UpsertUserGoalAsync(UserGoal userGoal, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserGoals
            .FirstOrDefaultAsync(g => g.UserId == userGoal.UserId, cancellationToken);

        if (existing == null)
        {
            await _context.UserGoals.AddAsync(userGoal, cancellationToken);
        }
        else
        {
            existing.DailyTarget = userGoal.DailyTarget;
            existing.GoalType = userGoal.GoalType;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCurrentStreakAsync(Guid userId, string timezone, CancellationToken cancellationToken = default)
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
        var yesterday = today.AddDays(-1);

        var activeDates = await _context.DailyLearningStats
            .Where(s => s.UserId == userId && (s.ReviewCount > 0 || s.LessonsCompleted > 0))
            .Select(s => s.LocalDate)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync(cancellationToken);

        if (!activeDates.Any())
        {
            return 0;
        }

        var mostRecent = activeDates.First();
        if (mostRecent != today && mostRecent != yesterday)
        {
            return 0;
        }

        int streak = 0;
        DateOnly expectedDate = mostRecent;

        foreach (var date in activeDates)
        {
            if (date == expectedDate)
            {
                streak++;
                expectedDate = expectedDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }
}
