using SOFIA.Application.Security.Commands.ForgotPassword;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Application.Security.Commands.Logout;
using SOFIA.Application.Security.Commands.Register;
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
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Ok(new { Token = result.Value })
            : Problem(result.Error.Message, statusCode: result.StatusCode);
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
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
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

        var result = await sender.Send(new LogoutCommand(cuentaId));
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Ok(new { Token = result.Value })
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterAccountCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Login), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}
