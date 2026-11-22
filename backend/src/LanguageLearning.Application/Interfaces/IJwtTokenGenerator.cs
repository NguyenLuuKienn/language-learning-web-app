using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string token, DateTime expiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
