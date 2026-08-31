using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Magistrales.Commands.CompletarOrdenMagistral;

public class CompletarOrdenMagistralCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CompletarOrdenMagistralCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CompletarOrdenMagistralCommand request, CancellationToken cancellationToken)
    {
        var orden = await dbContext.MagistralesOrdenesProduccion
            .FirstOrDefaultAsync(o => o.Id == request.OrdenId, cancellationToken);

        if (orden == null)
        {
            return Result.Failure<Guid>(Error.NotFound("Orden.NotFound", "Production order not found."));
        }

        if (orden.EstadoProduccion == "Completada")
        {
            return Result.Failure<Guid>(Error.Validation("Orden.Estado", "La orden ya ha sido completada anteriormente."));
        }

        if (!orden.CantidadProducida.HasValue || orden.CantidadProducida.Value <= 0)
        {
            return Result.Failure<Guid>(Error.Validation("Orden.Cantidad", "Order must have a valid produced quantity."));
        }

        // 1. Crear nuevo LoteInventario
        var loteResult = LoteInventario.Create(
            orden.ProductoResultanteId,
            request.NumeroLoteMfr,
            DateTimeOffset.UtcNow,
            request.FechaCaducidad
        );

        if (loteResult.IsFailure)
        {
            return Result.Failure<Guid>(loteResult.Error);
        }

        var nuevoLote = loteResult.Value;
        _ = _ = dbContext.LotesInventario.Add(nuevoLote!);

        // 2. Crear InventarioSucursal
        var inventarioResult = InventarioSucursal.Create(
            orden.SucursalId,
            nuevoLote!.Id,
            orden.CantidadProducida.Value
        );

        if (inventarioResult.IsFailure)
        {
            return Result.Failure<Guid>(inventarioResult.Error);
        }

        _ = _ = dbContext.LotesEnSucursal.Add(inventarioResult.Value!);

        // 3. Completar Orden
        _ = orden.Update(
            orden.SucursalId,
            orden.RecetaId,
            orden.ProductoResultanteId,
            nuevoLote!.Id,
            orden.CantidadProducida,
            orden.QuimicoPreparadorId,
            "Completada"
        );

        _ = dbContext.MagistralesOrdenesProduccion.Update(orden);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(orden!.Id);
    }
}
