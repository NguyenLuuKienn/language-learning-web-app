using System.Text;
using System.Text.Json;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LanguageLearning.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_ReturnsJwtWithAuthenticatedUserClaims()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] =
                    "test-secret-key-that-is-long-enough-for-hmac-sha256",
                ["Jwt:Issuer"] = "LanguageLearningApp",
                ["Jwt:Audience"] = "LanguageLearningAppClient"
            })
            .Build();
        var service = new JwtTokenService(configuration);

        var token = service.CreateAccessToken(user);
        var parts = token.Split('.');
        var payloadJson = Encoding.UTF8.GetString(
            DecodeBase64Url(parts[1]));
        using var payload = JsonDocument.Parse(payloadJson);

        Assert.Equal(3, parts.Length);
        Assert.Equal(user.Id.ToString(),
            payload.RootElement.GetProperty("sub").GetString());
        Assert.Equal(user.Email,
            payload.RootElement.GetProperty("email").GetString());
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var paddingLength = (4 - base64.Length % 4) % 4;

        return Convert.FromBase64String(base64.PadRight(
            base64.Length + paddingLength,
            '='));
    }
}
