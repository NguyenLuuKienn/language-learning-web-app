using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserWithHashedPasswordAndTokens()
    {
        var repository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHashService>();
        var tokenService = new Mock<ITokenService>();

        repository
            .Setup(item => item.GetByEmailAsync(
                "learner@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        passwordHasher
            .Setup(item => item.Hash("secure-password"))
            .Returns("password-hash");
        ConfigureTokens(tokenService);

        var service = CreateService(
            repository,
            passwordHasher,
            tokenService);

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "Learner@Example.com",
            Password = "secure-password",
            DisplayName = "Learner"
        });

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
        repository.Verify(item => item.AddAsync(
            It.Is<User>(user =>
                user.Email == "learner@example.com" &&
                user.PasswordHash == "password-hash" &&
                user.PasswordHash != "secure-password" &&
                user.RefreshTokens.Single().TokenHash == "refresh-token-hash"),
            It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(item => item.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsArgumentException()
    {
        var repository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHashService>();
        var tokenService = new Mock<ITokenService>();

        repository
            .Setup(item => item.GetByEmailAsync(
                "learner@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = "learner@example.com" });

        var service = CreateService(
            repository,
            passwordHasher,
            tokenService);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            service.RegisterAsync(new RegisterRequest
            {
                Email = "learner@example.com",
                Password = "secure-password"
            }));

        repository.Verify(item => item.AddAsync(
            It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokensAndStoresRefreshHash()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            PasswordHash = "password-hash"
        };
        var repository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHashService>();
        var tokenService = new Mock<ITokenService>();

        repository
            .Setup(item => item.GetByEmailAsync(
                user.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(item => item.Verify(user.PasswordHash, "secure-password"))
            .Returns(true);
        ConfigureTokens(tokenService);

        var service = CreateService(
            repository,
            passwordHasher,
            tokenService);

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "secure-password"
        });

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
        Assert.Contains(user.RefreshTokens,
            token => token.TokenHash == "refresh-token-hash");
        repository.Verify(item => item.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AuthService CreateService(
        Mock<IUserRepository> repository,
        Mock<IPasswordHashService> passwordHasher,
        Mock<ITokenService> tokenService)
    {
        return new AuthService(
            repository.Object,
            passwordHasher.Object,
            tokenService.Object);
    }

    private static void ConfigureTokens(Mock<ITokenService> tokenService)
    {
        tokenService
            .Setup(item => item.CreateAccessToken(It.IsAny<User>()))
            .Returns("access-token");
        tokenService
            .Setup(item => item.CreateRefreshToken())
            .Returns("refresh-token");
        tokenService
            .Setup(item => item.HashRefreshToken("refresh-token"))
            .Returns("refresh-token-hash");
        tokenService
            .Setup(item => item.GetAccessTokenExpiration())
            .Returns(DateTime.UtcNow.AddHours(1));
        tokenService
            .Setup(item => item.GetRefreshTokenExpiration())
            .Returns(DateTime.UtcNow.AddDays(7));
    }
}
