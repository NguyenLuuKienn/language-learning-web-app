using System.Security.Cryptography;
using System.Text;
using LanguageLearning.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LanguageLearning.Infrastructure.Security;

public sealed class PlacementProofService : IPlacementProofService
{
    private readonly byte[] _key;

    public PlacementProofService(IConfiguration configuration)
    {
        var secret = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string Create(Guid userId, Guid courseId)
    {
        var payload = $"v1:{userId:N}:{courseId:N}:{DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()}:{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var signature = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes("placement:" + payload));
        return $"{Convert.ToBase64String(bytes)}.{Convert.ToBase64String(signature)}";
    }

    public bool IsValid(string? proof, Guid userId, Guid courseId)
    {
        if (string.IsNullOrWhiteSpace(proof) || proof.Length > 512) return false;
        var parts = proof.Split('.', 2);
        if (parts.Length != 2) return false;
        try
        {
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var signature = Convert.FromBase64String(parts[1]);
            var fields = payload.Split(':');
            if (fields.Length != 5 || fields[0] != "v1"
                || !Guid.TryParseExact(fields[1], "N", out var subject) || subject != userId
                || !Guid.TryParseExact(fields[2], "N", out var course) || course != courseId
                || !long.TryParse(fields[3], out var expires)
                || expires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                || expires > DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()) return false;
            var expected = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes("placement:" + payload));
            return signature.Length == expected.Length && CryptographicOperations.FixedTimeEquals(signature, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
