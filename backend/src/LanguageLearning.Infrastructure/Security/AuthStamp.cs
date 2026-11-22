using System.Security.Cryptography;
using System.Text;

namespace LanguageLearning.Infrastructure.Security;

public static class AuthStamp
{
    public static string Compute(Guid userId, string passwordHash, string secretKey)
    {
        var key = Encoding.UTF8.GetBytes(secretKey);
        var data = Encoding.UTF8.GetBytes($"{userId:N}:{passwordHash}");
        return Convert.ToHexString(HMACSHA256.HashData(key, data));
    }

    public static bool Matches(string? supplied, string expected)
    {
        if (supplied?.Length != expected.Length) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(supplied), Encoding.ASCII.GetBytes(expected));
    }
}
