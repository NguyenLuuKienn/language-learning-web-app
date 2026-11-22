using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests.Controllers;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidLearnerCredentials_ReturnsSuccessAndToken()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest
        {
            Email = "learner@languagelearning.app",
            Password = "Learner@123456"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.AccessToken));
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("ll_access="));
        Assert.Equal("learner@languagelearning.app", result.User.Email);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest
        {
            Email = "learner@languagelearning.app",
            Password = "WrongPassword@123"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var problem = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("AUTH_INVALID_CREDENTIALS", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_WithUniqueEmail_ReturnsCreatedUser()
    {
        var client = _factory.CreateClient();
        var uniqueEmail = $"newuser_{Guid.NewGuid():N}@languagelearning.app";
        var request = new RegisterRequest
        {
            Email = uniqueEmail,
            Password = "SecurePassword123!",
            DisplayName = "New Student",
            Timezone = "Asia/Ho_Chi_Minh"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.Equal(uniqueEmail, result.User.Email);
        Assert.Equal("New Student", result.User.DisplayName);
    }

    [Fact]
    public async Task Register_RejectsShortPasswordAndBlankDisplayName()
    {
        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = $"invalid_{Guid.NewGuid():N}@example.com",
            Password = "short-password",
            DisplayName = "   "
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var validation = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("VALIDATION_ERROR", validation.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_RejectsInvalidEmailAndTrimmedShortName()
    {
        using var client = _factory.CreateClient();
        using var invalidEmail = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = "invalid address@example.com", Password = "SecurePassword123!", DisplayName = "Student"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidEmail.StatusCode);

        using var shortName = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = $"shortname_{Guid.NewGuid():N}@example.com", Password = "SecurePassword123!", DisplayName = " a "
        });
        Assert.Equal(HttpStatusCode.BadRequest, shortName.StatusCode);
    }

    [Fact]
    public async Task Auth_RejectsUnknownTimezoneOnRegistrationAndProfileUpdate()
    {
        using var anonymous = _factory.CreateClient();
        using var registration = await anonymous.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = $"timezone_{Guid.NewGuid():N}@example.com",
            Password = "SecurePassword123!",
            DisplayName = "Student",
            Timezone = "Invalid/Timezone"
        });
        Assert.Equal(HttpStatusCode.BadRequest, registration.StatusCode);

        using var learner = await _factory.CreateAuthenticatedClientAsync();
        using var update = await learner.PutAsJsonAsync("/api/v1/me", new UpdateProfileRequest
        {
            Timezone = "Invalid/Timezone"
        });
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task ProfileAndDailyGoal_UseValidatedEndpoints()
    {
        using var learner = await _factory.CreateAuthenticatedClientAsync();
        using var profileUpdate = await learner.PutAsJsonAsync("/api/v1/me", new UpdateProfileRequest
        {
            Timezone = "UTC"
        });
        Assert.Equal(HttpStatusCode.OK, profileUpdate.StatusCode);
        using var goalUpdate = await learner.PutAsJsonAsync("/api/v1/me/daily-goal", new UpdateDailyGoalRequest
        {
            DailyTarget = 12
        });
        Assert.Equal(HttpStatusCode.OK, goalUpdate.StatusCode);
        var profile = await learner.GetFromJsonAsync<UserProfileResponse>("/api/v1/me");
        Assert.NotNull(profile);
        Assert.Equal("UTC", profile.Timezone);
        Assert.Equal(12, profile.DailyTarget);
    }

    [Fact]
    public async Task Refresh_DoesNotAcceptTokenInJsonBody()
    {
        using var browser = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        using var login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "learner@languagelearning.app", Password = "Learner@123456"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var refreshCookie = login.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("ll_refresh="));
        var refreshToken = refreshCookie.Split(';', 2)[0].Split('=', 2)[1];
        using var rejected = await browser.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
    }

    [Fact]
    public async Task Refresh_RejectsReplayedCookie()
    {
        using var browser = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        using var login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "learner@languagelearning.app", Password = "Learner@123456"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookies = login.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0]).ToArray();
        browser.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));
        browser.DefaultRequestHeaders.Add("X-CSRF-Token", cookies.Single(value => value.StartsWith("ll_csrf=")).Split('=', 2)[1]);
        using var first = await browser.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var replayed = await browser.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.BadRequest, replayed.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithoutAuthorization_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserProfile()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("learner@languagelearning.app", profile.Email);
        Assert.Equal("Alex Johnson", profile.DisplayName);
    }

    [Fact]
    public async Task CookieSession_RequiresCsrfForWrites_AndLogsOut()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "learner@languagelearning.app", Password = "Learner@123456"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookies = login.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0]).ToArray();
        Assert.Contains(cookies, value => value.StartsWith("ll_access="));
        Assert.Contains(cookies, value => value.StartsWith("ll_refresh="));
        Assert.Contains(cookies, value => value.StartsWith("ll_csrf="));
        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));

        using var profile = await client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        using var rejected = await client.PutAsJsonAsync("/api/v1/me/daily-goal", new UpdateDailyGoalRequest { DailyTarget = 15 });
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);

        var csrf = cookies.Single(value => value.StartsWith("ll_csrf=")).Split('=', 2)[1];
        client.DefaultRequestHeaders.Add("X-CSRF-Token", csrf);
        using var accepted = await client.PutAsJsonAsync("/api/v1/me/daily-goal", new UpdateDailyGoalRequest { DailyTarget = 15 });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        using var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        using var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        Assert.NotEqual(HttpStatusCode.OK, refresh.StatusCode);
    }

    [Fact]
    public async Task UpdateDailyGoal_WithValidGoal_UpdatesSuccessfully()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var request = new UpdateDailyGoalRequest
        {
            DailyTarget = 25
        };

        var response = await client.PutAsJsonAsync("/api/v1/me/daily-goal", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal(25, profile.DailyTarget);
    }
}
