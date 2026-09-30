namespace SOFIA.Application.Common.Interfaces;

public interface IFrontendLinks
{
    // Secrets travel in the URL fragment, which browsers never send to servers or in Referer headers
    string PasswordReset(string token, string nombreUsuario);
}
