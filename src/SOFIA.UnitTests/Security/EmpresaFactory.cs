using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security;

internal static class EmpresaFactory
{
    // Builds a trial company whose Id matches the accounts' TenantId, as sign-up does
    public static Empresa WithId(Guid id)
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        empresa.SetId(id);
        return empresa;
    }
}
