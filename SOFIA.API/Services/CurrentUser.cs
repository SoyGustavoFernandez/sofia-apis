using System.Security.Claims;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.API.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? Id => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
                      ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("uid");

    public string? Name => httpContextAccessor.HttpContext?.User?.FindFirstValue("unique_name")
                        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
                        ?? httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
