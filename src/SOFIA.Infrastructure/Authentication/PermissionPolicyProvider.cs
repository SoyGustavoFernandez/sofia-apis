using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SOFIA.Infrastructure.Authentication;

/// <summary>
/// Dynamically provides authorization policies based on a specific naming convention (PERMISSION:Module:Action).
/// This avoids the need to manually register every possible permission as a policy.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    private const string PolicyPrefix = "PERMISSION";

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // If the policy name doesn't start with our prefix, use the default behavior.
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return await base.GetPolicyAsync(policyName);
        }

        // Expected format: PERMISSION:ModuleName:ActionName
        var parts = policyName.Split(':');
        if (parts.Length != 3)
        {
            return await base.GetPolicyAsync(policyName);
        }

        var module = parts[1];
        var action = parts[2];

        var policy = new AuthorizationPolicyBuilder();
        _ = policy.RequireAuthenticatedUser();
        _ = policy.AddRequirements(new PermissionRequirement(module, action));

        return policy.Build();
    }
}
