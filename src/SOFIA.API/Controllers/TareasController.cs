using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SOFIA.Application.Plataforma.Commands.EnviarAvisosVencimiento;
using SOFIA.Infrastructure.Email;

namespace SOFIA.API.Controllers;

// Called by the scheduler, which has no user account: it proves itself with a shared key
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TareasController(ISender sender, IOptions<NotificationOptions> options) : ControllerBase
{
    public const string KeyHeader = "X-Tareas-Key";

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.PublicInfoPolicy)]
    [HttpPost("avisos-vencimiento")]
    public async Task<IActionResult> EnviarAvisosVencimiento([FromHeader(Name = KeyHeader)] string? key, CancellationToken cancellationToken)
    {
        if (!IsValidKey(options.Value.TasksKey, key))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new EnviarAvisosVencimientoCommand(), cancellationToken);
        return result.ToActionResult();
    }

    // Hashes are compared in constant time, so neither the key nor its length leaks through timing
    private static bool IsValidKey(string expected, string? given) =>
        !string.IsNullOrWhiteSpace(expected) &&
        given is not null &&
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(given)));
}
