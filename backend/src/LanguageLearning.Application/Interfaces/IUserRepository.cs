using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserGoal?> GetUserGoalAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpsertUserGoalAsync(UserGoal userGoal, CancellationToken cancellationToken = default);
    Task<int> GetCurrentStreakAsync(Guid userId, string timezone, CancellationToken cancellationToken = default);
}
