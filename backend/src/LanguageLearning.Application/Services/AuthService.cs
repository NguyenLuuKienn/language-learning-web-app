using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var displayName = NormalizeDisplayName(request.DisplayName);
        ValidateNewPassword(request.Password);

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new BusinessRuleException(ErrorCodes.AuthEmailInUse, Messages.Auth.EmailInUse);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = displayName,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = UserRole.Learner,
            Timezone = ValidateTimezone(request.Timezone),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);

        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);

        var profile = new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            Timezone = user.Timezone,
            AvatarUrl = user.AvatarUrl,
            DailyTarget = 10,
            CurrentStreak = 0
        };

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt,
            User = profile
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BusinessRuleException(ErrorCodes.AuthInvalidCredentials, Messages.Auth.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            throw new BusinessRuleException(ErrorCodes.AuthAccountDeactivated, Messages.Auth.AccountDeactivated);
        }

        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);

        var userGoal = await _userRepository.GetUserGoalAsync(user.Id, cancellationToken);
        var streak = await _userRepository.GetCurrentStreakAsync(user.Id, user.Timezone, cancellationToken);

        var profile = new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            Timezone = user.Timezone,
            AvatarUrl = user.AvatarUrl,
            DailyTarget = userGoal?.DailyTarget ?? 10,
            CurrentStreak = streak
        };

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt,
            User = profile
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await _userRepository.GetRefreshTokenAsync(tokenHash, cancellationToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            throw new BusinessRuleException(ErrorCodes.AuthInvalidRefreshToken, Messages.Auth.InvalidRefreshToken);
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new BusinessRuleException(ErrorCodes.AuthAccountDeactivated, Messages.Auth.AccountDeactivated);
        }

        if (!await _userRepository.RevokeRefreshTokenAsync(storedToken, cancellationToken))
            throw new BusinessRuleException(ErrorCodes.AuthInvalidRefreshToken, Messages.Auth.InvalidRefreshToken);

        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddRefreshTokenAsync(newRefreshTokenEntity, cancellationToken);

        var userGoal = await _userRepository.GetUserGoalAsync(user.Id, cancellationToken);
        var streak = await _userRepository.GetCurrentStreakAsync(user.Id, user.Timezone, cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt,
            User = new UserProfileResponse
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role,
                Timezone = user.Timezone,
                AvatarUrl = user.AvatarUrl,
                DailyTarget = userGoal?.DailyTarget ?? 10,
                CurrentStreak = streak
            }
        };
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var tokenHash = HashToken(refreshToken);
        var storedToken = await _userRepository.GetRefreshTokenAsync(tokenHash, cancellationToken);
        if (storedToken != null && storedToken.IsActive)
        {
            await _userRepository.RevokeRefreshTokenAsync(storedToken, cancellationToken);
        }
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(Messages.Auth.UserNotFound);
        }

        var userGoal = await _userRepository.GetUserGoalAsync(user.Id, cancellationToken);
        var streak = await _userRepository.GetCurrentStreakAsync(user.Id, user.Timezone, cancellationToken);

        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            Timezone = user.Timezone,
            AvatarUrl = user.AvatarUrl,
            DailyTarget = userGoal?.DailyTarget ?? 10,
            CurrentStreak = streak
        };
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(Messages.Auth.UserNotFound);
        }

        if (request.DisplayName != null)
        {
            user.DisplayName = NormalizeDisplayName(request.DisplayName);
        }

        if (request.Timezone != null)
        {
            user.Timezone = ValidateTimezone(request.Timezone);
        }

        if (request.AvatarUrl != null)
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        return await GetProfileAsync(userId, cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidateNewPassword(request.NewPassword);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(Messages.Auth.UserNotFound);
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new BusinessRuleException(ErrorCodes.AuthIncorrectPassword, Messages.Auth.IncorrectPassword);
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.RevokeAllUserRefreshTokensAsync(userId, cancellationToken);
    }

    public async Task<int> UpdateDailyGoalAsync(Guid userId, int dailyTarget, CancellationToken cancellationToken = default)
    {
        var goal = await _userRepository.GetUserGoalAsync(userId, cancellationToken);
        if (goal == null)
        {
            goal = new UserGoal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DailyTarget = dailyTarget,
                GoalType = "CardsReview",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            goal.DailyTarget = dailyTarget;
            goal.UpdatedAt = DateTime.UtcNow;
        }

        await _userRepository.UpsertUserGoalAsync(goal, cancellationToken);
        return goal.DailyTarget;
    }

    private static string NormalizeEmail(string? email)
    {
        var value = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (value.Length == 0 || value.Length > 254 || value.Any(char.IsWhiteSpace)
            || !new EmailAddressAttribute().IsValid(value))
            throw new ArgumentException(Messages.Auth.EmailInvalid);
        return value;
    }

    private static string NormalizeDisplayName(string? displayName)
    {
        var value = displayName?.Trim() ?? string.Empty;
        if (value.Length < 2 || value.Length > 100 || value.Any(char.IsControl))
            throw new ArgumentException(Messages.Auth.DisplayNameInvalid);
        return value;
    }

    private static void ValidateNewPassword(string? password)
    {
        if (password == null || password.Length is < 15 or > 128 || password.Any(char.IsControl))
            throw new ArgumentException(Messages.Auth.PasswordInvalid);
    }

    private static string ValidateTimezone(string? timezone)
    {
        var value = string.IsNullOrWhiteSpace(timezone) ? "Asia/Ho_Chi_Minh" : timezone.Trim();
        if (value.Length > 100)
            throw new ArgumentException(Messages.Auth.TimezoneTooLong);
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(value);
            return value;
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException(Messages.Auth.TimezoneUnknown);
        }
        catch (InvalidTimeZoneException)
        {
            throw new ArgumentException(Messages.Auth.TimezoneInvalid);
        }
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
