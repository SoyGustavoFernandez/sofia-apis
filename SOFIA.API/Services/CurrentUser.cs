using SOFIA.Application.Common.Interfaces;

namespace SOFIA.API.Services;

public class CurrentUser : ICurrentUser
{
    // Placeholder implementation for now. 
    // This will be replaced by actual logic extracting data from HttpContext/Claims.
    public string? Id => "System";
    public string? Name => "System User";
    public bool IsAuthenticated => true;
}
