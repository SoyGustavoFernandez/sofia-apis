using SOFIA.Domain.Common;

namespace SOFIA.Application.Security;

public static class PasswordChangePolicy
{
    // Present while the account must change its password; the API then only serves the endpoints needed to do it
    public const string ClaimType = "pwd_change";

    public static readonly Error RequiredError =
        Error.Forbidden("Auth.CambioClaveRequerido", "You must change your password before continuing.");
}
