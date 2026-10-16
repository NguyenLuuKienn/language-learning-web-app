
using System.Security.Claims;
using LanguageLearning.Api.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace LanguageLearning.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public void GetRequiredUserId_ValidSubClaim_ReturnsUserId()
    {
        var userId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim("sub", userId.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "Test");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var service = new CurrentUserService(accessor);

        var result = service.GetRequiredUserId();

        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetRequiredUserId_NameIdentifierClaim_ReturnsUserId()
    {
        var userId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                userId.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "Test");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var service = new CurrentUserService(accessor);

        var result = service.GetRequiredUserId();

        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetRequiredUserId_UnauthenticatedUser_ThrowsUnauthorized()
    {
        var context = new DefaultHttpContext();

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var service = new CurrentUserService(accessor);

        Assert.Throws<UnauthorizedAccessException>(
            () => service.GetRequiredUserId());
    }

    [Fact]
    public void GetRequiredUserId_InvalidUserId_ThrowsUnauthorized()
    {
        var claims = new[]
        {
            new Claim("sub", "invalid-guid")
        };

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "Test");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var service = new CurrentUserService(accessor);

        Assert.Throws<UnauthorizedAccessException>(
            () => service.GetRequiredUserId());
    }
}
