using Microsoft.AspNetCore.Authorization;

namespace SOFIA.Infrastructure.Authentication;

/// <summary>Grants access with the permission or, lacking it, to the platform operator, whose work must not depend on its role inside its own company.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionOrPlataformaAttribute(string module, string action)
    : AuthorizeAttribute(policy: $"{PermissionPolicyProvider.PlatformPolicyPrefix}:{module}:{action}")
{
    public string Module { get; } = module;
    public string Action { get; } = action;
}
