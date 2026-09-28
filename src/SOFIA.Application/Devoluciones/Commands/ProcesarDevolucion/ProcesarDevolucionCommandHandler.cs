using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Devoluciones.Commands.ProcesarDevolucion;

public class ProcesarDevolucionCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<ProcesarDevolucionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProcesarDevolucionCommand request, CancellationToken cancellationToken)
    {
        // The authorizer is whoever is logged in; a supervisor authorizes by signing in themselves
        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return Result.Failure<Guid>(empleadoResult.Error);
        }

        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<Guid>(sucursalResult.Error);
        }

        var venta = await dbContext.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == request.ComprobanteOrigenId, cancellationToken);

        if (venta == null)
        {
            return Result.Failure<Guid>(Error.NotFound("Venta.NotFound", "La venta original no fue encontrada."));
        }

        if (venta.SucursalId != sucursalResult.Value)
        {
            return Result.Failure<Guid>(Error.Forbidden("Devolucion.Venta.OtraSucursal", "Only sales of your own branch can be returned."), 403);
        }

        // The refund leaves the caller's own open drawer at this branch, like any sale they collect
        var sesion = await dbContext.POSSesionesCaja
            .FirstOrDefaultAsync(s => s.EmpleadoId == empleadoResult.Value && s.SucursalId == sucursalResult.Value && s.EstadoSesion == EstadoSesion.Abierta && !s.IsDeleted, cancellationToken);

        if (sesion == null)
        {
            return Result.Failure<Guid>(Error.Validation("Devolucion.Caja.SinSesionAbierta", "You need an open cash register session to process a return."));
        }

        var documentosResult = await ResolveDocumentosFiscalesAsync(venta, cancellationToken);
        if (documentosResult.IsFailure)
        {
            return Result.Failure<Guid>(documentosResult.Error);
        }

        var (comprobanteOrigen, serieNc) = documentosResult.Value;

        var cantidadesADevolver = request.Detalles
            .GroupBy(d => d.DetalleVentaId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.CantidadDevuelta));

        var cantidadesYaDevueltas = await GetCantidadesYaDevueltasAsync(venta, cancellationToken);

        var montoResult = venta.RegistrarDevolucion(cantidadesADevolver, cantidadesYaDevueltas);
        if (montoResult.IsFailure)
        {
            return Result.Failure<Guid>(montoResult.Error);
        }

        var detallesResult = BuildDomainDetalles(request.Detalles);
        if (detallesResult.IsFailure)
        {
            return Result.Failure<Guid>(detallesResult.Error);
        }

        var cabeceraResult = DevolucionCabecera.Create(
            comprobanteOrigen.Id,
            Guid.Empty,
            empleadoResult.Value,
            request.MotivoSunatCatalogo,
            request.SustentoDescriptivo,
            DateTime.UtcNow,
            detallesResult.Value!);

        if (cabeceraResult.IsFailure)
        {
            return Result.Failure<Guid>(cabeceraResult.Error);
        }

        var devolucion = cabeceraResult.Value!;

        var montoReembolso = await CalcularReembolsoAsync(venta, montoResult.Value, cancellationToken);
        var reembolsoResult = devolucion.RegistrarReembolso(sesion.Id, montoReembolso, request.MetodoReembolso);
        if (reembolsoResult.IsFailure)
        {
            return Result.Failure<Guid>(reembolsoResult.Error);
        }

        await RestoreInventoryAsync(request.Detalles, venta, cancellationToken);

        _ = dbContext.Devoluciones.Add(devolucion);

        var ncResult = await GenerateCreditNoteAsync(venta, comprobanteOrigen, serieNc, devolucion, montoResult.Value, cancellationToken);
        if (ncResult.IsFailure)
        {
            return Result.Failure<Guid>(ncResult.Error);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(devolucion.Id);
    }

    // The return credits the sale's issued boleta/factura (never a previous credit note) through the branch's active NC series
    private async Task<Result<(SunatComprobanteEmitido Origen, SunatSerieFiscal SerieNc)>> ResolveDocumentosFiscalesAsync(Venta venta, CancellationToken cancellationToken)
    {
        var comprobanteOrigen = await (
            from c in dbContext.SUNATComprobantesEmitidos
            join s in dbContext.SUNATSeriesFiscales on c.SerieId equals s.Id
            where c.TransaccionId == venta.Id && !c.IsDeleted && (s.TipoComprobante == TipoComprobante.Boleta || s.TipoComprobante == TipoComprobante.Factura)
            orderby c.FechaEmision
            select c).FirstOrDefaultAsync(cancellationToken);

        if (comprobanteOrigen == null)
        {
            return Result.Failure<(SunatComprobanteEmitido, SunatSerieFiscal)>(Error.Validation("Devolucion.ComprobanteOrigen.NoEmitido", "The sale has no issued boleta or factura to credit."));
        }

        var serieNc = await dbContext.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.SucursalId == venta.SucursalId && s.TipoComprobante == TipoComprobante.NotaCredito && s.EstadoSerie == SunatSerieFiscal.EstadoActiva && !s.IsDeleted, cancellationToken);

        return serieNc == null
            ? Result.Failure<(SunatComprobanteEmitido, SunatSerieFiscal)>(Error.Validation("Devolucion.SerieNotaCredito.NoConfigurada", "The branch has no active credit note series."))
            : Result.Success((comprobanteOrigen, serieNc));
    }

    private async Task<Dictionary<Guid, decimal>> GetCantidadesYaDevueltasAsync(Venta venta, CancellationToken cancellationToken)
    {
        var detalleIds = venta.Detalles.Select(d => d.Id).ToList();

        var devueltas = await (
            from dd in dbContext.DetallesDevolucion
            join dc in dbContext.Devoluciones on dd.DevolucionId equals dc.Id
            where detalleIds.Contains(dd.DetalleVentaId) && !dd.IsDeleted && !dc.IsDeleted
            select new { dd.DetalleVentaId, dd.CantidadDevuelta }).ToListAsync(cancellationToken);

        return devueltas
            .GroupBy(d => d.DetalleVentaId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.CantidadDevuelta));
    }

    // The customer gets back only their share; the insurer-covered part of the credited amount is not paid out
    private async Task<decimal> CalcularReembolsoAsync(Venta venta, decimal montoDevuelto, CancellationToken cancellationToken)
    {
        if (venta.MontoTotalBruto <= 0)
        {
            return 0m;
        }

        var detalleIds = venta.Detalles.Select(d => d.Id).ToList();
        var montoCubierto = await dbContext.VentasReclamosSeguro
            .Where(r => detalleIds.Contains(r.DetalleVentaId) && !r.IsDeleted)
            .SumAsync(r => r.MontoCubierto, cancellationToken);

        var proporcionCliente = Math.Clamp((venta.MontoTotalBruto - montoCubierto) / venta.MontoTotalBruto, 0m, 1m);
        return Math.Round(montoDevuelto * proporcionCliente, 2);
    }

    private static Result<List<DevolucionDetalle>> BuildDomainDetalles(List<DevolucionDetalleDto> dtos)
    {
        var domainDetalles = new List<DevolucionDetalle>();

        foreach (var dto in dtos)
        {
            var detalleResult = DevolucionDetalle.Create(dto.DetalleVentaId, dto.CantidadDevuelta, dto.DestinoFisicoLogico);
            if (detalleResult.IsFailure)
            {
                return Result.Failure<List<DevolucionDetalle>>(detalleResult.Error);
            }

            domainDetalles.Add(detalleResult.Value!);
        }

        return Result.Success(domainDetalles);
    }

    private async Task RestoreInventoryAsync(List<DevolucionDetalleDto> dtos, Venta venta, CancellationToken cancellationToken)
    {
        foreach (var dto in dtos)
        {
            if (dto.DestinoFisicoLogico != DestinoDevolucion.Reingreso_Venta)
            {
                continue;
            }

            var ventaDetalle = venta.Detalles.First(d => d.Id == dto.DetalleVentaId);
            var inventario = await dbContext.LotesEnSucursal
                .FirstOrDefaultAsync(i => i.SucursalId == venta.SucursalId && i.LoteId == ventaDetalle.LoteId, cancellationToken);

            if (inventario != null)
            {
                inventario.AddStock(dto.CantidadDevuelta);
                _ = dbContext.LotesEnSucursal.Update(inventario);
            }
        }
    }

    // The credit note bills only the returned lines and names the same customer as the document it reverses
    private async Task<Result> GenerateCreditNoteAsync(Venta venta, SunatComprobanteEmitido origen, SunatSerieFiscal serie, DevolucionCabecera devolucion, decimal montoDevuelto, CancellationToken cancellationToken)
    {
        var correlativo = await dbContext.IncrementarCorrelativoSunatAsync(serie.Id, cancellationToken);
        var (gravado, igv) = VentaComprobanteGenerator.DesglosarIgv(montoDevuelto);

        var ncResult = SunatComprobanteEmitido.Create(
            venta.Id, serie.Id, correlativo,
            origen.TipoDocIdentidadCliente, origen.NumeroIdentidadCliente, origen.RazonSocialCliente,
            gravado, 0m, igv, montoDevuelto,
            null, "Aceptado", null, null, null,
            DateTime.UtcNow);

        if (ncResult.IsFailure)
        {
            return Result.Failure(ncResult.Error);
        }

        _ = dbContext.SUNATComprobantesEmitidos.Add(ncResult.Value!);
        return devolucion.Update(devolucion.ComprobanteOrigenId, ncResult.Value!.Id, devolucion.EmpleadoAutorizaId, devolucion.MotivoSunatCatalogo, devolucion.SustentoDescriptivo, devolucion.FechaDevolucion);
    }
}
