using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Ventas.Common;

public static class VentaSeguroProcessor
{
    public static void Process(IApplicationDbContext context, Guid primerDetalleId, Guid aseguradoraId, decimal montoCubierto, decimal montoTotalBruto)
    {
        var reclamoResult = VentaReclamoSeguro.Create(
            primerDetalleId,
            aseguradoraId,
            montoCubierto,
            montoTotalBruto - montoCubierto,
            "Aprobado",
            "AUTH_" + Guid.NewGuid().ToString("N")[..8]);

        if (reclamoResult.IsSuccess)
        {
            _ = context.VentasReclamosSeguro.Add(reclamoResult.Value);
        }
    }
}
