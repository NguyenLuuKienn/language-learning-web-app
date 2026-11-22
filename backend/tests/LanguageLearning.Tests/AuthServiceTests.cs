using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Security;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasher = new PasswordHasher();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        _jwtTokenGeneratorMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns(("mock.access.token", DateTime.UtcNow.AddHours(1)));

        _jwtTokenGeneratorMock.Setup(j => j.GenerateRefreshToken())
            .Returns("mock-refresh-token-123456");

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasher,
            _jwtTokenGeneratorMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnAuthResponse()
    {
        var request = new RegisterRequest
        {
            Email = "student@test.com",
            Password = "SecurePassword123!",
            DisplayName = "Test Student",
            Timezone = "Asia/Ho_Chi_Minh"
        };

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        var result = await _authService.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal("mock.access.token", result.AccessToken);
        Assert.Equal("mock-refresh-token-123456", result.RefreshToken);
        Assert.Equal("student@test.com", result.User.Email);
        Assert.Equal("Test Student", result.User.DisplayName);
        Assert.Equal(UserRole.Learner, result.User.Role);

        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "student@test.com"), default), Times.Once);
        _userRepositoryMock.Verify(r => r.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnErrorCode()
    {
        var request = new RegisterRequest
        {
            Email = "existing@test.com",
            Password = "SecurePassword123!",
            DisplayName = "Duplicate"
        };

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync("existing@test.com", default))
            .ReturnsAsync(true);

        var error = await Assert.ThrowsAsync<LanguageLearning.Application.Common.Exceptions.BusinessRuleException>(() => _authService.RegisterAsync(request));
        Assert.Equal("AUTH_EMAIL_IN_USE", error.Code);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ShouldReturnTokens()
    {
        var password = "SecurePassword123!";
        var passwordHash = _passwordHasher.HashPassword(password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            PasswordHash = passwordHash,
            DisplayName = "Valid User",
            IsActive = true,
            Role = UserRole.Learner
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("user@test.com", default))
            .ReturnsAsync(user);

        var loginRequest = new LoginRequest
        {
            Email = "user@test.com",
            Password = password
        };

        var result = await _authService.LoginAsync(loginRequest);

        Assert.NotNull(result);
        Assert.Equal("mock.access.token", result.AccessToken);
        Assert.Equal(user.Id, result.User.Id);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldReturnErrorCode()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            PasswordHash = _passwordHasher.HashPassword("RealPassword123!"),
            DisplayName = "Valid User",
            IsActive = true
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("user@test.com", default))
            .ReturnsAsync(user);

        var loginRequest = new LoginRequest
        {
            Email = "user@test.com",
            Password = "WrongPassword!"
        };

        var error = await Assert.ThrowsAsync<LanguageLearning.Application.Common.Exceptions.BusinessRuleException>(() => _authService.LoginAsync(loginRequest));
        Assert.Equal("AUTH_INVALID_CREDENTIALS", error.Code);
    }

    [Fact]
    public void PasswordHasher_Verify_ShouldWorkCorrectly()
    {
        var hasher = new PasswordHasher();
        var raw = "MyStrongPassword@2026";

        var hash = hasher.HashPassword(raw);

        Assert.True(hasher.VerifyPassword(raw, hash));
        Assert.False(hasher.VerifyPassword("WrongPassword", hash));
        Assert.StartsWith("600000.", hash);
        Assert.False(hasher.VerifyPassword(raw, "600000.invalid-base64.invalid-base64"));
        Assert.False(hasher.VerifyPassword(raw, "2147483647.AAAA.AAAA"));
    }
}
