using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;

public record InsumoDto(Guid InventarioSucursalId, decimal CantidadConsumida);

public record IniciarOrdenMagistralCommand(
    Guid SucursalId,
    Guid? RecetaId,
    Guid ProductoResultanteId,
    Guid QuimicoPreparadorId,
    decimal? CantidadProducida,
    List<InsumoDto> Consumos
) : ICommand<Guid>;

public class IniciarOrdenMagistralCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<IniciarOrdenMagistralCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(IniciarOrdenMagistralCommand request, CancellationToken cancellationToken)
    {
        // 1. Crear Orden de Producción
        var ordenResult = MagistralOrdenProduccion.Create(
            request.SucursalId,
            request.RecetaId,
            request.ProductoResultanteId,
            null, // LoteGeneradoId is null until completed
            request.CantidadProducida,
            request.QuimicoPreparadorId,
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
            var inventario = await dbContext.LotesEnSucursal
                .Include(i => i.Lote)
                .FirstOrDefaultAsync(i => i.Id == dto.InventarioSucursalId, cancellationToken);

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
