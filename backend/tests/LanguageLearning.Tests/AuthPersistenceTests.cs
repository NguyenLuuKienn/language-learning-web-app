using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Services;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.Infrastructure.Repositories;
using LanguageLearning.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LanguageLearning.Tests;

public class AuthPersistenceTests
{
    [Fact]
    public async Task Login_PersistsNewRefreshTokenForExistingUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] =
                    "test-secret-key-that-is-long-enough-for-hmac-sha256"
            })
            .Build();

        await using (var context = new AppDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var service = CreateService(context, configuration);
            await service.RegisterAsync(new RegisterRequest
            {
                Email = "learner@example.com",
                Password = "secure-password"
            });
        }

        await using (var context = new AppDbContext(options))
        {
            var service = CreateService(context, configuration);
            await service.LoginAsync(new LoginRequest
            {
                Email = "learner@example.com",
                Password = "secure-password"
            });
        }

        await using (var context = new AppDbContext(options))
        {
            var user = await context.Users
                .Include(item => item.RefreshTokens)
                .SingleAsync();

            Assert.Equal(2, user.RefreshTokens.Count);
        }
    }

    private static AuthService CreateService(
        AppDbContext context,
        IConfiguration configuration)
    {
        return new AuthService(
            new UserRepository(context),
            new Pbkdf2PasswordHashService(),
            new JwtTokenService(configuration));
    }
}
