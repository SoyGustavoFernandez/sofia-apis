using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;

public class IniciarOrdenMagistralCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<IniciarOrdenMagistralCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(IniciarOrdenMagistralCommand request, CancellationToken cancellationToken)
    {
        // The preparing chemist and the branch whose stock is consumed come from the session
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<Guid>(sucursalResult.Error);
        }

        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return Result.Failure<Guid>(empleadoResult.Error);
        }

        var sucursalId = sucursalResult.Value;

        if (!await dbContext.Medicamentos.AnyAsync(m => m.Id == request.ProductoResultanteId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Medicamento.NotFound", "The specified product does not exist."), 404);
        }

        if (request.RecetaId is { } recetaId
            && !await dbContext.Recetas.AnyAsync(r => r.Id == recetaId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("RecetaMedica.NotFound", "Receta médica not found."), 404);
        }

        // 1. Create production order
        var ordenResult = MagistralOrdenProduccion.Create(
            sucursalId,
            request.RecetaId,
            request.ProductoResultanteId,
            null, // LoteGeneradoId is null until completed
            request.CantidadProducida,
            empleadoResult.Value,
            "Iniciada",
            DateTime.UtcNow
        );

        if (ordenResult.IsFailure)
        {
            return Result.Failure<Guid>(ordenResult.Error);
        }

        var orden = ordenResult.Value;

        // 2. Descontar Insumos y Guardar Consumos
        foreach (var dto in request.Consumos)
        {
            // Only the operator's own branch stock may be consumed
            var inventario = await dbContext.LotesEnSucursal
                .Include(i => i.Lote)
                .FirstOrDefaultAsync(i => i.Id == dto.InventarioSucursalId && i.SucursalId == sucursalId, cancellationToken);

            if (inventario == null)
            {
                return Result.Failure<Guid>(Error.NotFound("Inventario.NotFound", $"Inventario ID {dto.InventarioSucursalId} no encontrado."));
            }

            if (inventario.CantidadFisica < dto.CantidadConsumida)
            {
                return Result.Failure<Guid>(Error.Validation("Inventario.StockInsuficiente", $"Stock insuficiente para el inventario {dto.InventarioSucursalId}. Stock actual: {inventario.CantidadFisica}"));
            }

            // Deduct inventory
            inventario.UpdateStock(inventario.CantidadFisica - dto.CantidadConsumida);
            _ = dbContext.LotesEnSucursal.Update(inventario);

            // Record consumption
            var consumoResult = MagistralConsumoInsumo.Create(
                orden!.Id,
                inventario.LoteId,
                dto.CantidadConsumida,
                "UND"
            );

            if (consumoResult.IsFailure)
            {
                return Result.Failure<Guid>(consumoResult.Error);
            }

            // (OrdenProduccionId already set in Create)
            // orden._consumos is private, but EF tracks it if we save it directly via DbContext or through the navigation property
            _ = _ = dbContext.MagistralesConsumosInsumo.Add(consumoResult.Value!);
        }

        _ = _ = dbContext.MagistralesOrdenesProduccion.Add(orden!);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(orden!.Id);
    }
}
