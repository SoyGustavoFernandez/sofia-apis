using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Ventas.Common;

// Company and branch that issue the receipt, so sales tests reach the comprobante with a RUC by default
internal static class EmisorMocks
{
    public const string Ruc = "20212971893";
    public const string RazonSocial = "Botica QA";
    public const string Direccion = "Av. Arequipa 123";

    public static void Setup(Mock<IApplicationDbContext> dbContextMock, Guid sucursalId, string? ruc = Ruc)
    {
        var empresa = Empresa.Create(RazonSocial, ruc).Value!;
        var sucursal = Sucursal.Create("Central", Direccion, "LIC-1", null, empresa.Id).Value!;
        sucursal.SetId(sucursalId);
        _ = dbContextMock.Setup(c => c.Empresas).Returns(new List<Empresa> { empresa }.BuildMockDbSet().Object);
        _ = dbContextMock.Setup(c => c.Sucursales).Returns(new List<Sucursal> { sucursal }.BuildMockDbSet().Object);
    }
}
