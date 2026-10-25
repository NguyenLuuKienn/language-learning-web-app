using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace LanguageLearning.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly byte[] _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _accessTokenLifetime;
    private readonly TimeSpan _refreshTokenLifetime;

    public JwtTokenService(IConfiguration configuration)
    {
        var secretKey = configuration["Jwt:SecretKey"];

        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey must contain at least 32 characters.");
        }

        _signingKey = Encoding.UTF8.GetBytes(secretKey);
        _issuer = configuration["Jwt:Issuer"] ?? "LanguageLearningApp";
        _audience = configuration["Jwt:Audience"] ?? "LanguageLearningAppClient";
        _accessTokenLifetime = TimeSpan.FromMinutes(
            GetPositiveInteger(configuration["Jwt:ExpirationMinutes"], 60));
        _refreshTokenLifetime = TimeSpan.FromDays(
            GetPositiveInteger(
                configuration["Jwt:RefreshTokenExpirationDays"],
                7));
    }

    public string CreateAccessToken(User user)
    {
        var expiresAt = GetAccessTokenExpiration();
        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "HS256",
            typ = "JWT"
        }));
        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = user.Id.ToString(),
            email = user.Email,
            role = user.Role.ToString(),
            iss = _issuer,
            aud = _audience,
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = new DateTimeOffset(expiresAt).ToUnixTimeSeconds()
        }));
        var unsignedToken = $"{header}.{payload}";
        var signature = HMACSHA256.HashData(
            _signingKey,
            Encoding.UTF8.GetBytes(unsignedToken));

        return $"{unsignedToken}.{Base64UrlEncode(signature)}";
    }

    public string CreateRefreshToken()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    }

    public string HashRefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        return Convert.ToBase64String(SHA256.HashData(
            Encoding.UTF8.GetBytes(refreshToken)));
    }

    public DateTime GetAccessTokenExpiration()
    {
        return DateTime.UtcNow.Add(_accessTokenLifetime);
    }

    public DateTime GetRefreshTokenExpiration()
    {
        return DateTime.UtcNow.Add(_refreshTokenLifetime);
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static int GetPositiveInteger(string? value, int fallback)
    {
        return int.TryParse(value, out var parsedValue) && parsedValue > 0
            ? parsedValue
            : fallback;
    }
}
