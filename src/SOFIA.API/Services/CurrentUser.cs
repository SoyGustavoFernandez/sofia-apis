using System.Security.Claims;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.API.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? Id => httpContextAccessor.HttpContext?.User?.FindFirstValue("empleadoId")
                      ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
                      ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Name => httpContextAccessor.HttpContext?.User?.FindFirstValue("unique_name")
                        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
                        ?? httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public string? SucursalId => httpContextAccessor.HttpContext?.User?.FindFirstValue("sucursalId");

    public string? EmpresaId => httpContextAccessor.HttpContext?.User?.FindFirstValue("empresaId");

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    // UseForwardedHeaders already rewrote this from X-Forwarded-For, but only for trusted proxies
    public string? ClientIpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    // Same claim lookup as PermissionAuthorizationHandler, so both agree on who is an Admin
    public bool IsInRole(string roleName) =>
        httpContextAccessor.HttpContext?.User?.Claims
            .Any(c => c.Type == ClaimTypes.Role && c.Value.Equals(roleName, StringComparison.OrdinalIgnoreCase)) ?? false;
}
