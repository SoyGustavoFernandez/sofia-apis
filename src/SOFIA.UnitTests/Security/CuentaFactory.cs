using System.Reflection;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security;

internal static class CuentaFactory
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    // Builds an account whose Empleado.Sucursal_Base navigation is loaded, as login and refresh do
    public static Cuenta WithBaseBranch(Guid? cuentaTenantId, Guid? sucursalTenantId, string usuario = "usuario", string passwordHash = "hash")
    {
        var sucursal = Sucursal.Create("Sede", "Av. Siempre Viva 123", "LIC-001", empresaId: sucursalTenantId).Value!;
        var empleado = Empleado.Create(sucursal.Id, "Ana", "Pérez", "Gómez", tenantId: cuentaTenantId).Value!;
        typeof(Empleado).GetField("<Sucursal_Base>k__BackingField", PrivateInstance)!.SetValue(empleado, sucursal);

        var cuenta = Cuenta.Create(empleado.Id, usuario, passwordHash, cuentaTenantId).Value!;
        typeof(Cuenta).GetField("<Empleado>k__BackingField", PrivateInstance)!.SetValue(cuenta, empleado);
        return cuenta;
    }
}
