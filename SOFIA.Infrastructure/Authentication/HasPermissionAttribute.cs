using Microsoft.AspNetCore.Authorization;

namespace SOFIA.Infrastructure.Authentication;

/// <summary>
/// A convenience attribute to apply permission-based authorization to controllers or actions.
/// Usage: [HasPermission("Module", "Action")]
/// </summary>
public sealed class HasPermissionAttribute(string module, string action) : AuthorizeAttribute(policy: $"PERMISSION:{module}:{action}")
{
}
