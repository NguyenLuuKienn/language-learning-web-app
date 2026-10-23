using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHashService passwordHashService,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHashService = passwordHashService;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        ValidatePassword(request.Password);

        var existingUser = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (existingUser != null)
        {
            throw new DuplicateEmailException(email);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = GetDisplayName(request.DisplayName, email),
            PasswordHash = _passwordHashService.Hash(request.Password)
        };

        var response = CreateTokenResponse(user);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user == null || !user.IsActive ||
            !_passwordHashService.Verify(user.PasswordHash, request.Password))
        {
            throw new ArgumentException("Invalid email or password.");
        }

        var response = CreateTokenResponse(user);

        await _userRepository.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await GetActiveRefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);
        var user = refreshToken.User;

        refreshToken.RevokedAt = DateTime.UtcNow;
        var response = CreateTokenResponse(user);

        await _userRepository.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var refreshToken = await _userRepository.GetRefreshTokenByHashAsync(
            tokenHash,
            cancellationToken);

        if (refreshToken == null || refreshToken.IsRevoked)
        {
            return;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    private AuthResponse CreateTokenResponse(User user)
    {
        var refreshToken = _tokenService.CreateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            User = user,
            TokenHash = _tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = _tokenService.GetRefreshTokenExpiration()
        });

        return new AuthResponse
        {
            AccessToken = _tokenService.CreateAccessToken(user),
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = _tokenService.GetAccessTokenExpiration()
        };
    }

    private async Task<RefreshToken> GetActiveRefreshTokenAsync(
        string refreshTokenValue,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            throw new UnauthorizedAccessException(
                "A valid refresh token is required.");
        }

        var tokenHash = _tokenService.HashRefreshToken(refreshTokenValue);
        var refreshToken = await _userRepository.GetRefreshTokenByHashAsync(
            tokenHash,
            cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive ||
            !refreshToken.User.IsActive)
        {
            throw new UnauthorizedAccessException(
                "A valid refresh token is required.");
        }

        return refreshToken;
    }

    private static string NormalizeEmail(string email)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            throw new ArgumentException("A valid email address is required.");
        }

        return normalizedEmail;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException(
                "Password must contain at least 8 characters.");
        }
    }

    private static string GetDisplayName(string displayName, string email)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        return email.Split('@')[0];
    }
}
