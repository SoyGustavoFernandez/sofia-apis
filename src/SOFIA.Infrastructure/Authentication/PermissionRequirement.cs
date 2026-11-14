using Microsoft.AspNetCore.Authorization;

namespace SOFIA.Infrastructure.Authentication;

/// <summary>Represents a permission requirement that consists of a Module and an Action; AllowPlatformOperator also lets the platform operator through.</summary>
public sealed class PermissionRequirement(string module, string action, bool allowPlatformOperator = false) : IAuthorizationRequirement
{
    public string Module { get; } = module;
    public string Action { get; } = action;
    public bool AllowPlatformOperator { get; } = allowPlatformOperator;
}
