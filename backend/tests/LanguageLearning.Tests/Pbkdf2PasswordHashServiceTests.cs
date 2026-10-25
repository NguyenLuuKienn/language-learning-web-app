using LanguageLearning.Infrastructure.Security;
using Xunit;

namespace LanguageLearning.Tests;

public class Pbkdf2PasswordHashServiceTests
{
    [Fact]
    public void Hash_ProducesVerifiableValueWithoutPlaintextPassword()
    {
        var service = new Pbkdf2PasswordHashService();

        var passwordHash = service.Hash("secure-password");

        Assert.DoesNotContain("secure-password", passwordHash);
        Assert.True(service.Verify(passwordHash, "secure-password"));
        Assert.False(service.Verify(passwordHash, "invalid-password"));
    }
}
