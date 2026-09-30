namespace SOFIA.Infrastructure.Email;

public class AppOptions
{
    public const string SectionName = "App";

    public string FrontendBaseUrl { get; init; } = string.Empty;

    public void EnsureValid(bool allowHttp)
    {
        if (!Uri.TryCreate(FrontendBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !(allowHttp && uri.Scheme == Uri.UriSchemeHttp)))
        {
            throw new InvalidOperationException("CRITICAL: App:FrontendBaseUrl must be an absolute https URL of the SPA (http is only accepted in Development and Testing).");
        }
    }
}
