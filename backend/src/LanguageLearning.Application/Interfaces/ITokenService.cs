using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTime GetAccessTokenExpiration();
    DateTime GetRefreshTokenExpiration();
}
