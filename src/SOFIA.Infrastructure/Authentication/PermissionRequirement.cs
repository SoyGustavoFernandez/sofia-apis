using Microsoft.AspNetCore.Authorization;

namespace SOFIA.Infrastructure.Authentication;

/// <summary>
/// Represents a permission requirement that consists of a Module and an Action.
/// </summary>
public sealed class PermissionRequirement(string module, string action) : IAuthorizationRequirement
{
    public string Module { get; } = module;
    public string Action { get; } = action;
}
