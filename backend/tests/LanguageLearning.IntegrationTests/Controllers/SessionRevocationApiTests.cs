using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests.Controllers;

public class SessionRevocationApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SessionRevocationApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task DeactivatedUser_ExistingAccessTokenStopsWorking()
    {
        using var learner = _factory.CreateClient();
        var register = await learner.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = $"revoke_{Guid.NewGuid():N}@example.com",
            Password = "long-secure-passphrase-2026",
            DisplayName = "Session Test"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        learner.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
            register.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("ll_access=")).Split(';', 2)[0].Split('=', 2)[1]);
        Assert.Equal(HttpStatusCode.OK, (await learner.GetAsync("/api/v1/me")).StatusCode);

        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        using var deactivation = await admin.PatchAsJsonAsync($"/api/v1/admin/users/{auth.User.Id}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivation.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await learner.GetAsync("/api/v1/me")).StatusCode);
    }

    [Fact]
    public async Task PasswordChange_RevokesExistingAccessToken()
    {
        using var learner = _factory.CreateClient();
        var email = $"password_{Guid.NewGuid():N}@example.com";
        const string oldPassword = "long-secure-passphrase-2026";
        const string newPassword = "another-long-passphrase-2026";
        using var register = await learner.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = oldPassword,
            DisplayName = "Password Test"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        learner.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
            register.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("ll_access=")).Split(';', 2)[0].Split('=', 2)[1]);

        using var changed = await learner.PostAsJsonAsync("/api/v1/me/change-password", new ChangePasswordRequest
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await learner.GetAsync("/api/v1/me")).StatusCode);

        using var freshLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, freshLogin.StatusCode);
    }
}
