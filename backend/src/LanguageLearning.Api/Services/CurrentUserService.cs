
using System.Security.Claims;
using LanguageLearning.Application.Interfaces;

namespace LanguageLearning.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetRequiredUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }

        var userIdValue =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId) ||
            userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "A valid user identity is required.");
        }

        return userId;
    }
}
