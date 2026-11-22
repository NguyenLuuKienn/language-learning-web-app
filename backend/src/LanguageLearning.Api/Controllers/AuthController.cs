using LanguageLearning.Application.Common;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        SetSessionCookies(response);
        return Ok(new { response.User, response.ExpiresAt });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        SetSessionCookies(response);
        return Ok(new { response.User, response.ExpiresAt });
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("session")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["ll_refresh"];
        if (string.IsNullOrWhiteSpace(refreshToken)) return Unauthorized();
        var response = await _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshToken }, cancellationToken);
        SetSessionCookies(response);
        return Ok(new { response.User, response.ExpiresAt });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(Request.Cookies["ll_refresh"], cancellationToken);
        Response.Cookies.Delete("ll_access");
        Response.Cookies.Delete("ll_refresh");
        Response.Cookies.Delete("ll_csrf");
        return Ok(new { message = Messages.Auth.LogoutSuccess });
    }

    private void SetSessionCookies(AuthResponse response)
    {
        var secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
        Response.Cookies.Append("ll_access", response.AccessToken, new CookieOptions
        {
            HttpOnly = true, Secure = secure, SameSite = SameSiteMode.Strict, Path = "/", Expires = response.ExpiresAt
        });
        Response.Cookies.Append("ll_refresh", response.RefreshToken, new CookieOptions
        {
            HttpOnly = true, Secure = secure, SameSite = SameSiteMode.Strict, Path = "/api/v1/auth", Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
        Response.Cookies.Append("ll_csrf", Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), new CookieOptions
        {
            HttpOnly = false, Secure = secure, SameSite = SameSiteMode.Strict, Path = "/", Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
