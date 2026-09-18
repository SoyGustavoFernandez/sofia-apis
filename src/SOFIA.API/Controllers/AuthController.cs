using Microsoft.AspNetCore.RateLimiting;
using SOFIA.Application.Security.Commands.ForgotPassword;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Application.Security.Commands.Logout;
using SOFIA.Application.Security.Commands.RefreshToken;
using SOFIA.Application.Security.Commands.Register;
using SOFIA.Application.Security.Commands.ResetPassword;
using SOFIA.Application.Security.Queries.GetProfile;
using System.Security.Claims;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController(ISender sender) : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await sender.Send(command);
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);
        return Ok(new { result.Value.AccessToken });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        var rawToken = Request.Cookies[RefreshTokenCookieName];
        if (rawToken is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new RefreshTokenCommand(rawToken));
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);
        return Ok(new { result.Value.AccessToken });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var cuentaId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new GetProfileQuery(cuentaId));
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var cuentaId))
        {
            return Unauthorized();
        }

        Response.Cookies.Delete(RefreshTokenCookieName);
        var result = await sender.Send(new LogoutCommand(cuentaId));
        return result.ToActionResult();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        _ = await sender.Send(command);
        return Ok(new { Message = "If the account exists, a recovery link will be sent to the registered contact." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await sender.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterAccountCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Login), new { id = result.Value }, result.Value)
            : result.ToProblemResult();
    }

    private void SetRefreshTokenCookie(string token, DateTimeOffset expiry) => Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = expiry.UtcDateTime
    });
}
