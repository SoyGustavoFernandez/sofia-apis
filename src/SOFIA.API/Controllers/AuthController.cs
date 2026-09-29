using Microsoft.AspNetCore.RateLimiting;
using SOFIA.API.Infrastructure;
using SOFIA.Application.Security.Commands.ChangePassword;
using SOFIA.Application.Security.Commands.ForgotPassword;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Application.Security.Commands.Logout;
using SOFIA.Application.Security.Commands.RefreshToken;
using SOFIA.Application.Security.Commands.ResetPassword;
using SOFIA.Application.Security.Queries.GetProfile;
using System.Security.Claims;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await sender.Send(command);
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        Response.AppendRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);
        return Ok(new { result.Value.AccessToken, result.Value.RequiereCambioClave });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        var rawToken = Request.Cookies[RefreshTokenCookieExtensions.CookieName];
        if (rawToken is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new RefreshTokenCommand(rawToken));
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        Response.AppendRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);
        return Ok(new { result.Value.AccessToken, result.Value.RequiereCambioClave });
    }

    [Authorize]
    [AllowDuringPasswordChange]
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

    // Anonymous so an expired access token cannot leave the refresh cookie (and the session) alive on a shared PC
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        Guid? cuentaId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        var rawToken = Request.Cookies[RefreshTokenCookieExtensions.CookieName];

        Response.DeleteRefreshTokenCookie();
        var result = await sender.Send(new LogoutCommand(cuentaId, rawToken));
        return result.ToActionResult();
    }

    // Ends every session of the account: the client logs in again with the new password
    [HttpPost("change-password")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [Authorize]
    [AllowDuringPasswordChange]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var cuentaId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new ChangePasswordCommand(cuentaId, request.CurrentPassword, request.NewPassword));
        if (result.IsSuccess)
        {
            Response.DeleteRefreshTokenCookie();
        }

        return result.ToActionResult();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        _ = await sender.Send(command);
        return Ok(new { Message = "If the account exists, a recovery link will be sent to the registered contact." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await sender.Send(command);
        return result.ToActionResult();
    }
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
