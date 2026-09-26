namespace SOFIA.Application.Common.Models;

/// <summary>
/// Names of the global EF Core query filters, used to selectively bypass them via IgnoreQueryFilters([...]).
/// </summary>
public static class QueryFilters
{
    public const string SoftDelete = "SoftDelete";

    // Only bypass in flows that run before a tenant is known (login, token refresh, sign-up, background jobs)
    public const string Tenant = "Tenant";
}
