using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;

namespace SOFIA.IntegrationTests.Infrastructure;

// Builds a valid public sign-up with a unique RUC and admin email, since both are required and RUC is globally unique
public static class RegistroEmpresaBuilder
{
    public const string Password = "TestPassword123!";

    public static RegistrarEmpresaCommand Nuevo(string usuario) => new()
    {
        NombreEmpresa = $"Farmacia {usuario}",
        RUC = string.Concat(Enumerable.Range(0, 11).Select(_ => Random.Shared.Next(10))),
        Usuario = usuario,
        Password = Password,
        AdminNombres = "Admin",
        AdminApellidoPaterno = "Prueba",
        AdminEmail = $"{usuario}@sofia.test",
        DireccionSede = "Av. Los Olivos 123, Lima",
    };
}
