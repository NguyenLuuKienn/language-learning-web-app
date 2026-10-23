using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LanguageLearning.Tests;

public class AuthApiTests
{
    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequestProblemDetails()
    {
        var repository = new InMemoryUserRepository();
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();
        var request = new RegisterRequest
        {
            Email = "learner@example.com",
            Password = "secure-password",
            DisplayName = "Learner"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            request);
        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.StartsWith("application/problem+json",
            duplicateResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsJwtAndRefreshToken()
    {
        var repository = new InMemoryUserRepository();
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest
            {
                Email = "learner@example.com",
                Password = "secure-password"
            });
        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest
            {
                Email = "learner@example.com",
                Password = "secure-password"
            });
        var response = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(response);
        Assert.Equal(3, response.AccessToken.Split('.').Length);
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.NotEqual(response.RefreshToken,
            repository.GetUser("learner@example.com")!
                .RefreshTokens.Last().TokenHash);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        InMemoryUserRepository repository)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting(
                    "Jwt:SecretKey",
                    "test-secret-key-that-is-long-enough-for-hmac-sha256");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IUserRepository>();
                    services.AddSingleton<IUserRepository>(repository);
                });
            });
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _users =
            new(StringComparer.OrdinalIgnoreCase);
        private User? _pendingUser;

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            _users.TryGetValue(email, out var user);

            return Task.FromResult(user);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            _pendingUser = user;

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            if (_pendingUser != null)
            {
                _users.Add(_pendingUser.Email, _pendingUser);
                _pendingUser = null;
            }

            return Task.CompletedTask;
        }

        public User? GetUser(string email)
        {
            _users.TryGetValue(email, out var user);

            return user;
        }
    }
}
