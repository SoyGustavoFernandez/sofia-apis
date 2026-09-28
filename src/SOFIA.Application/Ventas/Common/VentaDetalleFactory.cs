using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Common;

public static class VentaDetalleFactory
{
    private sealed record ProductoVendible(Guid ProductoId, decimal? PrecioVentaBase);

    private sealed record PrecioLinea(decimal CantidadBase, decimal PrecioUnitarioBase, Guid? PresentacionId, decimal? CantidadEnPresentacion);

    // ventaIdExcluida is the pending sale whose lines are being replaced, so its own prescription use is not counted twice
    public static async Task<Result<List<DetalleVenta>>> BuildAsync(IApplicationDbContext context, List<CreateVentaDetailDto> dtos, Guid sucursalId, Guid? clienteId, Guid? ventaIdExcluida, CancellationToken cancellationToken)
    {
        var detallesVenta = new List<DetalleVenta>();
        var recetas = new Dictionary<Guid, RecetaMedica>();
        var condicionesPorReceta = new Dictionary<Guid, HashSet<CondicionVenta>>();
        var ahora = DateTimeOffset.UtcNow;

        foreach (var detailDto in dtos)
        {
            var productoResult = await ResolveProductoAsync(context, detailDto, clienteId, ahora, recetas, condicionesPorReceta, cancellationToken);
            if (productoResult.IsFailure)
            {
                return Result.Failure<List<DetalleVenta>>(productoResult.Error);
            }

            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detailDto.LoteId && x.SucursalId == sucursalId, cancellationToken);

            if (inventario == null)
            {
                return Result.Failure<List<DetalleVenta>>(Error.NotFound("Venta.Lote", $"El lote {detailDto.LoteId} no existe en esta sucursal."));
            }

            var precioResult = await ResolvePrecioAsync(context, detailDto, productoResult.Value!, cancellationToken);
            if (precioResult.IsFailure)
            {
                return Result.Failure<List<DetalleVenta>>(precioResult.Error);
            }

            var precio = precioResult.Value!;

            if (inventario.CantidadFisica < precio.CantidadBase)
            {
                return Result.Failure<List<DetalleVenta>>(Error.Validation("Venta.Stock", $"Stock insuficiente para el lote {detailDto.LoteId}. Disponible: {inventario.CantidadFisica}"));
            }

            var costoHistorico = await GetCostoVigenteAsync(context, productoResult.Value!.ProductoId, cancellationToken);

            var detailResult = DetalleVenta.Create(detailDto.LoteId, precio.CantidadBase, precio.PrecioUnitarioBase, costoHistorico, detailDto.RecetaId, precio.PresentacionId, precio.CantidadEnPresentacion);
            if (!detailResult.IsSuccess)
            {
                return Result.Failure<List<DetalleVenta>>(detailResult.Error);
            }

            inventario.UpdateStock(inventario.CantidadFisica - precio.CantidadBase);
            detallesVenta.Add(detailResult.Value);
        }

        var dispensacionResult = await ValidateDispensacionesAsync(context, recetas, condicionesPorReceta, ventaIdExcluida, cancellationToken);

        return dispensacionResult.IsFailure
            ? Result.Failure<List<DetalleVenta>>(dispensacionResult.Error)
            : Result.Success(detallesVenta);
    }

    // The lot must be sellable (not quarantined, not expired) and its product's sale condition met by the prescription
    private static async Task<Result<ProductoVendible>> ResolveProductoAsync(IApplicationDbContext context, CreateVentaDetailDto detailDto, Guid? clienteId, DateTimeOffset ahora, Dictionary<Guid, RecetaMedica> recetas, Dictionary<Guid, HashSet<CondicionVenta>> condicionesPorReceta, CancellationToken cancellationToken)
    {
        var enCuarentena = await context.DigemidInventarioCuarentena
            .AnyAsync(q => q.LoteId == detailDto.LoteId && q.EstadoResolucion == EstadoResolucionCuarentena.Retenido && !q.IsDeleted, cancellationToken);

        if (enCuarentena)
        {
            return Result.Failure<ProductoVendible>(Error.Validation("Venta.Cuarentena", $"Lot {detailDto.LoteId} is in quarantine and cannot be sold under any circumstances."));
        }

        var lote = await context.LotesInventario
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == detailDto.LoteId, cancellationToken);

        if (lote is null)
        {
            return Result.Failure<ProductoVendible>(Error.NotFound("Venta.Lote", $"El lote {detailDto.LoteId} no existe en esta sucursal."));
        }

        if (lote.EstaVencido(ahora))
        {
            return Result.Failure<ProductoVendible>(Error.Validation("Venta.Lote.Vencido", $"Lot {lote.NumeroLoteMfr} is expired and cannot be sold."));
        }

        var producto = await context.Medicamentos
            .Where(m => m.Id == lote.ProductoId)
            .Select(m => new { m.CondicionVenta, m.PrecioVentaBase })
            .FirstOrDefaultAsync(cancellationToken);

        if (producto is null)
        {
            return Result.Failure<ProductoVendible>(Error.NotFound("Venta.Lote", $"El lote {detailDto.LoteId} no existe en esta sucursal."));
        }

        var recetaResult = await ResolveRecetaAsync(context, detailDto.RecetaId, producto.CondicionVenta, clienteId, recetas, cancellationToken);
        if (recetaResult.IsFailure)
        {
            return Result.Failure<ProductoVendible>(recetaResult.Error);
        }

        if (detailDto.RecetaId.HasValue && producto.CondicionVenta != CondicionVenta.VentaLibreOTC)
        {
            _ = condicionesPorReceta.TryAdd(detailDto.RecetaId.Value, []);
            _ = condicionesPorReceta[detailDto.RecetaId.Value].Add(producto.CondicionVenta);
        }

        return Result.Success(new ProductoVendible(lote.ProductoId, producto.PrecioVentaBase));
    }

    // Cantidad is in base units unless a sale presentation (e.g. "Caja x10") was picked; the backend, never the client, resolves conversion and price
    private static async Task<Result<PrecioLinea>> ResolvePrecioAsync(IApplicationDbContext context, CreateVentaDetailDto detailDto, ProductoVendible producto, CancellationToken cancellationToken)
    {
        if (!detailDto.PresentacionVentaId.HasValue)
        {
            return producto.PrecioVentaBase is null
                ? Result.Failure<PrecioLinea>(Error.Validation("Venta.SinPrecio", $"El producto del lote {detailDto.LoteId} no tiene precio de venta configurado."))
                : Result.Success(new PrecioLinea(detailDto.Cantidad, producto.PrecioVentaBase.Value, null, null));
        }

        var presentacion = await context.PresentacionesVenta
            .FirstOrDefaultAsync(p => p.Id == detailDto.PresentacionVentaId.Value && !p.IsDeleted, cancellationToken);

        if (presentacion == null)
        {
            return Result.Failure<PrecioLinea>(Error.NotFound("Venta.Presentacion", $"La presentacion de venta {detailDto.PresentacionVentaId.Value} no existe."));
        }

        if (presentacion.ProductoId != producto.ProductoId)
        {
            return Result.Failure<PrecioLinea>(Error.Validation("Venta.Presentacion", "La presentacion seleccionada no corresponde al producto del lote."));
        }

        return Result.Success(new PrecioLinea(
            detailDto.Cantidad * presentacion.CantidadUnidadesBase,
            presentacion.PrecioVenta / presentacion.CantidadUnidadesBase,
            presentacion.Id,
            detailDto.Cantidad));
    }

    // Prescription-only products need a prescription of the sale's own client; an id sent for an OTC product is still checked
    private static async Task<Result> ResolveRecetaAsync(IApplicationDbContext context, Guid? recetaId, CondicionVenta condicion, Guid? clienteId, Dictionary<Guid, RecetaMedica> recetas, CancellationToken cancellationToken)
    {
        if (recetaId is null)
        {
            return condicion == CondicionVenta.VentaLibreOTC
                ? Result.Success()
                : Result.Failure(Error.Validation("Venta.Receta.Requerida", "This product requires a medical prescription."));
        }

        if (!recetas.TryGetValue(recetaId.Value, out var receta))
        {
            receta = await context.Recetas
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == recetaId.Value && !r.IsDeleted, cancellationToken);

            if (receta is null)
            {
                return Result.Failure(Error.Validation("Venta.Receta.NoEncontrada", "The medical prescription does not exist."));
            }

            recetas[recetaId.Value] = receta;
        }

        return clienteId is null || receta.ClienteId != clienteId
            ? Result.Failure(Error.Validation("Venta.Receta.OtroPaciente", "The medical prescription belongs to another patient."))
            : Result.Success();
    }

    // A sale is one dispensation however many lines it has; voided sales give their dispensation back
    private static async Task<Result> ValidateDispensacionesAsync(IApplicationDbContext context, Dictionary<Guid, RecetaMedica> recetas, Dictionary<Guid, HashSet<CondicionVenta>> condicionesPorReceta, Guid? ventaIdExcluida, CancellationToken cancellationToken)
    {
        foreach (var (recetaId, condiciones) in condicionesPorReceta)
        {
            var dispensacionesPrevias = await (
                from d in context.DetallesVenta
                join v in context.Ventas on d.VentaId equals v.Id
                join l in context.LotesInventario on d.LoteId equals l.Id
                join m in context.Medicamentos on l.ProductoId equals m.Id
                where d.RecetaId == recetaId
                      && v.Estado != EstadoVenta.Anulada
                      && !v.IsDeleted
                      && v.Id != ventaIdExcluida
                      && m.CondicionVenta != CondicionVenta.VentaLibreOTC
                select d.VentaId)
                .Distinct()
                .CountAsync(cancellationToken);

            var agotada = condiciones
                .Select(condicion => recetas[recetaId].PuedeDispensar(dispensacionesPrevias, condicion))
                .FirstOrDefault(r => r.IsFailure);

            if (agotada is not null)
            {
                return agotada;
            }
        }

        return Result.Success();
    }

    // Cost snapshot from the supplier price in force at sale time; 0 when the product has no supplier price yet
    private static async Task<decimal> GetCostoVigenteAsync(IApplicationDbContext context, Guid productoId, CancellationToken cancellationToken)
    {
        var ahora = DateTime.UtcNow;

        return await context.HistorialPreciosProveedor
            .Where(h => h.ProductoId == productoId
                     && h.FechaInicioVigencia <= ahora
                     && (h.FechaFinVigencia == null || h.FechaFinVigencia >= ahora))
            .OrderByDescending(h => h.FechaInicioVigencia)
            .Select(h => (decimal?)h.CostoPorUnidadBase)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;
    }
}
