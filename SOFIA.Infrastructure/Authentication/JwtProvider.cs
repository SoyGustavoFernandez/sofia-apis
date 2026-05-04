using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SOFIA.Infrastructure.Authentication;

public class JwtProvider(IOptions<JwtOptions> options) : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;

    public string Generate(Cuenta cuenta)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, cuenta.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, cuenta.NombreUsuario),
            new("empleadoId", cuenta.EmpleadoId.ToString()),
            new("securityStamp", cuenta.SecurityStamp.ToString())
        };

        // Agregar roles como claims
        foreach (var rol in cuenta.Roles)
        {
            claims.Add(new(ClaimTypes.Role, rol.NombreRol));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            null,
            DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
