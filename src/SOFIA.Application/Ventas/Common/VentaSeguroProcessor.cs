using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Ventas.Common;

public static class VentaSeguroProcessor
{
    // No-op unless the sale carries an insurer and a coverage amount; a claim that cannot be created fails the sale
    public static Result Process(IApplicationDbContext context, Venta venta, Guid? aseguradoraId, decimal? montoCubiertoSeguro)
    {
        if (aseguradoraId == null || montoCubiertoSeguro == null || venta.Detalles.Count == 0)
        {
            return Result.Success();
        }

        var reclamoResult = VentaReclamoSeguro.Create(
            venta.Detalles.First().Id,
            aseguradoraId.Value,
            montoCubiertoSeguro.Value,
            venta.MontoTotalBruto - montoCubiertoSeguro.Value,
            "Aprobado",
            "AUTH_" + Guid.NewGuid().ToString("N")[..8]);

        if (reclamoResult.IsFailure)
        {
            return Result.Failure(reclamoResult.Error);
        }

        _ = context.VentasReclamosSeguro.Add(reclamoResult.Value!);

        return Result.Success();
    }

    // Coverage counts only with an insurer of this tenant; without one it is zero, an unknown one fails the sale
    public static async Task<Result<decimal>> ResolveCoberturaAsync(IApplicationDbContext context, Guid? aseguradoraId, decimal? montoCubiertoSeguro, CancellationToken cancellationToken)
    {
        if (aseguradoraId == null)
        {
            return Result.Success(0m);
        }

        var existe = await context.Aseguradoras.AnyAsync(a => a.Id == aseguradoraId.Value && !a.IsDeleted, cancellationToken);

        return existe
            ? Result.Success(montoCubiertoSeguro ?? 0)
            : Result.Failure<decimal>(Error.NotFound("Aseguradora.NotFound", $"Aseguradora with ID {aseguradoraId} was not found."), 404);
    }
}
