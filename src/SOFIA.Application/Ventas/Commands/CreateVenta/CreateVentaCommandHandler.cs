using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using System.Text.Json;

namespace SOFIA.Application.Ventas.Commands.CreateVenta;

public class CreateVentaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CreateVentaCommand, Result<VentaCreadaDto>>
{
    public async Task<Result<VentaCreadaDto>> Handle(CreateVentaCommand request, CancellationToken cancellationToken)
    {
        var contextResult = ValidateUserContext(out var sucursalId, out var empleadoId);
        if (contextResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(contextResult.Error);
        }

        var sesionResult = await ValidateSesionCajaAsync(request.SesionId, cancellationToken);
        if (sesionResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(sesionResult.Error);
        }

        var detallesResult = await BuildVentaDetallesAsync(request.Detalles, sucursalId, cancellationToken);
        if (detallesResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(detallesResult.Error);
        }

        var ventaResult = Venta.Create(sucursalId, empleadoId, request.ClienteId, request.SesionId, detallesResult.Value!, request.Estado);
        if (!ventaResult.IsSuccess)
        {
            return Result.Failure<VentaCreadaDto>(ventaResult.Error);
        }

        _ = context.Ventas.Add(ventaResult.Value);

        var dtoComprobante = await GenerateComprobanteAsync(ventaResult.Value, sucursalId, cancellationToken);

        if (request.AseguradoraId != null && request.MontoCubiertoSeguro != null && detallesResult.Value!.Count > 0)
        {
            ProcessInsurance(detallesResult.Value![0].Id, request, ventaResult.Value.MontoTotalBruto);
        }

        CreateOutboxEvent(ventaResult.Value.Id, sucursalId);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new VentaCreadaDto(ventaResult.Value.Id, dtoComprobante), 201);
    }

    private Result ValidateUserContext(out Guid sucursalId, out Guid empleadoId)
    {
        sucursalId = Guid.Empty;
        empleadoId = Guid.Empty;

        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId) || string.IsNullOrEmpty(currentUser.Id))
        {
            return Result.Failure(Error.Unauthorized("Venta.Auth", "User must be authenticated and assigned to a branch."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out sucursalId))
        {
            return Result.Failure(Error.Validation("Venta.Sucursal", "Invalid Sucursal ID in user context."));
        }

        if (!Guid.TryParse(currentUser.Id, out empleadoId))
        {
            return Result.Failure(Error.Validation("Venta.Empleado", "Invalid Empleado ID in user context."));
        }

        return Result.Success();
    }

    private async Task<Result> ValidateSesionCajaAsync(Guid? sesionId, CancellationToken cancellationToken)
    {
        if (sesionId == null)
        {
            return Result.Failure(Error.Validation("Venta.Caja", "A cash register session is required to process the sale."));
        }

        var sesionCaja = await context.POSSesionesCaja
            .FirstOrDefaultAsync(x => x.Id == sesionId && !x.IsDeleted, cancellationToken);

        return sesionCaja == null || sesionCaja.EstadoSesion != EstadoSesion.Abierta
            ? Result.Failure(Error.Validation("Venta.Caja", "The cash register session is not open or does not exist."))
            : Result.Success();
    }

    private async Task<Result<List<DetalleVenta>>> BuildVentaDetallesAsync(List<CreateVentaDetailDto> dtos, Guid sucursalId, CancellationToken cancellationToken)
    {
        var detallesVenta = new List<DetalleVenta>();

        foreach (var detailDto in dtos)
        {
            var enCuarentena = await context.DigemidInventarioCuarentena
                .AnyAsync(q => q.LoteId == detailDto.LoteId && q.EstadoResolucion == "Retenido" && !q.IsDeleted, cancellationToken);

            if (enCuarentena)
            {
                return Result.Failure<List<DetalleVenta>>(Error.Validation("Venta.Cuarentena", $"Lot {detailDto.LoteId} is in quarantine and cannot be sold under any circumstances."));
            }

            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detailDto.LoteId && x.SucursalId == sucursalId, cancellationToken);

            if (inventario == null)
            {
                return Result.Failure<List<DetalleVenta>>(Error.NotFound("Venta.Lote", $"El lote {detailDto.LoteId} no existe en esta sucursal."));
            }

            if (inventario.CantidadFisica < detailDto.Cantidad)
            {
                return Result.Failure<List<DetalleVenta>>(Error.Validation("Venta.Stock", $"Stock insuficiente para el lote {detailDto.LoteId}. Disponible: {inventario.CantidadFisica}"));
            }

            var detailResult = DetalleVenta.Create(detailDto.LoteId, detailDto.Cantidad, detailDto.PrecioUnitario, detailDto.CostoHistorico, detailDto.RecetaId);
            if (!detailResult.IsSuccess)
            {
                return Result.Failure<List<DetalleVenta>>(detailResult.Error);
            }

            inventario.UpdateStock(inventario.CantidadFisica - detailDto.Cantidad);
            detallesVenta.Add(detailResult.Value);
        }

        return Result.Success(detallesVenta);
    }

    private async Task<ComprobanteEmitidoDto?> GenerateComprobanteAsync(Venta venta, Guid sucursalId, CancellationToken cancellationToken)
    {
        var serie = await context.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.SucursalId == sucursalId && s.TipoComprobante == TipoComprobante.Boleta && s.EstadoSerie == "Activa" && !s.IsDeleted, cancellationToken);

        if (serie == null)
        {
            var newSerieResult = SunatSerieFiscal.Create(sucursalId, TipoComprobante.Boleta, "B001", 0, "Activa");
            if (newSerieResult.IsSuccess)
            {
                serie = newSerieResult.Value;
                _ = context.SUNATSeriesFiscales.Add(serie);
            }
        }

        if (serie == null)
        {
            return null;
        }

        var correlativo = serie.CorrelativoActual + 1;
        _ = serie.Update(serie.SucursalId, serie.TipoComprobante, serie.PrefijoSerie, correlativo, serie.EstadoSerie);

        var total = venta.MontoTotalBruto;
        var comprobanteResult = SunatComprobanteEmitido.Create(
            venta.Id, serie.Id, correlativo,
            "1", "00000000", "CLIENTE EVENTUAL",
            total * 0.82m, 0, total * 0.18m, total,
            "HASH_SIMULATED_" + Guid.NewGuid().ToString("N")[..8],
            "Aceptado",
            $"/comprobantes/XML_{correlativo}.xml",
            $"/comprobantes/CDR_{correlativo}.xml",
            $"https://sunat.gob.pe/verificar/{serie.PrefijoSerie}-{correlativo}");

        if (!comprobanteResult.IsSuccess)
        {
            return null;
        }

        _ = context.SUNATComprobantesEmitidos.Add(comprobanteResult.Value);
        return new ComprobanteEmitidoDto(
            serie.TipoComprobante.ToString(),
            $"{serie.PrefijoSerie}-{correlativo:D8}",
            "Aceptado",
            comprobanteResult.Value.UrlPublicaVerificacion,
            comprobanteResult.Value.RutaArchivoXml,
            comprobanteResult.Value.RutaArchivoCdr);
    }

    private void ProcessInsurance(Guid primerDetalleId, CreateVentaCommand request, decimal montoTotalBruto)
    {
        var reclamoResult = VentaReclamoSeguro.Create(
            primerDetalleId,
            request.AseguradoraId!.Value,
            request.MontoCubiertoSeguro!.Value,
            montoTotalBruto - request.MontoCubiertoSeguro.Value,
            "Aprobado",
            "AUTH_" + Guid.NewGuid().ToString("N")[..8]);

        if (reclamoResult.IsSuccess)
        {
            _ = context.VentasReclamosSeguro.Add(reclamoResult.Value);
        }
    }

    private void CreateOutboxEvent(Guid ventaId, Guid sucursalId)
    {
        var outboxResult = SistemaOutboxEvento.Create(
            "VentaCompletada",
            JsonSerializer.Serialize(new { VentaId = ventaId, SucursalId = sucursalId }),
            false, null, null);

        if (outboxResult.IsSuccess)
        {
            _ = context.SistemaOutboxEventos.Add(outboxResult.Value);
        }
    }
}
