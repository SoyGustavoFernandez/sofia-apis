using SOFIA.Domain.Common;

namespace SOFIA.Application.Security;

public static class PlataformaAdminPolicy
{
    public const string Name = "PLATAFORMA_ADMIN";

    // Present only while the account is flagged as platform operator in the database
    public const string ClaimType = "plat_admin";

    public static readonly Error RequiredError =
        Error.Forbidden("Auth.PlataformaAdminRequerido", "Only the platform operator can perform this action.");
}
