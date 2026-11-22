using System.Security.Claims;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetRequiredUserId()
    {
        var userId = GetUserId();
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User is not authenticated or identity is invalid.");
        }

        return userId.Value;
    }

    public Guid? GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdValue = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdValue, out var userId) && userId != Guid.Empty)
        {
            return userId;
        }

        return null;
    }

    public bool IsAdmin()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        return string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
