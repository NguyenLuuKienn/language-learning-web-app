using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests.Controllers;

public class LearningAccessApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LearningAccessApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/v1/courses")]
    [InlineData("/api/v1/courses/33333333-3333-3333-3333-333333333333/material")]
    [InlineData("/api/v1/lessons/33333333-3333-3333-3333-333333333333")]
    [InlineData("/api/v1/vocabularies")]
    [InlineData("/api/v1/lessons/33333333-3333-3333-3333-333333333333/quiz")]
    [InlineData("/api/v1/decks/33333333-3333-3333-3333-333333333333/cards")]
    [InlineData("/api/v1/public/decks")]
    public async Task LearningContent_RequiresLogin(string path)
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Languages_RemainsPublic()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/languages");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownOrigin_DoesNotReceiveCorsPermission()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/languages");
        request.Headers.Add("Origin", "https://untrusted.example");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task RepeatedFailedLogins_AreRateLimited()
    {
        using var client = _factory.CreateClient();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "invalid-password"
            });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using var limited = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "invalid-password"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}
