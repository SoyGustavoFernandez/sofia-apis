namespace SOFIA.API.Infrastructure;

// Marks an authenticated endpoint that stays reachable while the account must change its password
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowDuringPasswordChangeAttribute : Attribute;
