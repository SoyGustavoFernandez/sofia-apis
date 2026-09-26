namespace SOFIA.API.Extensions;

public static class RefreshTokenCookieExtensions
{
    public const string CookieName = "refresh_token";

    // SameSite=None: the SPA and the API live on different sites (scheme/domain), Strict would drop the cookie
    public static void AppendRefreshTokenCookie(this HttpResponse response, string token, DateTimeOffset expiry) =>
        response.Cookies.Append(CookieName, token, BuildOptions(expiry.UtcDateTime));

    // Deletion must repeat the same attributes, otherwise the browser rejects the cross-site Set-Cookie
    public static void DeleteRefreshTokenCookie(this HttpResponse response) =>
        response.Cookies.Delete(CookieName, BuildOptions(expires: null));

    private static CookieOptions BuildOptions(DateTime? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Expires = expires,
    };
}
