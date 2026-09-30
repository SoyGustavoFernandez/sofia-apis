using Microsoft.Extensions.Options;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Email;

public class FrontendLinks(IOptions<AppOptions> options) : IFrontendLinks
{
    public string PasswordReset(string token, string nombreUsuario) =>
        $"{options.Value.FrontendBaseUrl.TrimEnd('/')}/auth/reset-password#token={Uri.EscapeDataString(token)}&usuario={Uri.EscapeDataString(nombreUsuario)}";
}
