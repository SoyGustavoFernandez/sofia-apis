using Microsoft.AspNetCore.Authorization;

namespace SOFIA.Infrastructure.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string module, string action) : AuthorizeAttribute(policy: $"PERMISSION:{module}:{action}")
{
}
