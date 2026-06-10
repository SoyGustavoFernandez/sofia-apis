using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using System.Text.Json;

namespace SOFIA.Application.Ventas.Commands.CreateVenta;

public record CreateVentaCommand(
    Guid? ClienteId,
    Guid? SesionId,
    List<CreateVentaDetailDto> Detalles,
    EstadoVenta Estado = EstadoVenta.Completada,
    Guid? AseguradoraId = null,
    decimal? MontoCubiertoSeguro = null) : IRequest<Result<VentaCreadaDto>>;

public record VentaCreadaDto(Guid VentaId, ComprobanteEmitidoDto? Comprobante);

// NOTA: La integración SUNAT es simulada (tesis). Los campos de hash, URL y CDR son placeholders.
// La integración real requiere un OSE/PSE homologado y firma digital con certificado.
public record ComprobanteEmitidoDto(string Tipo, string Numero, string EstadoAceptacion, string? UrlVerificacion, string? UrlXml, string? UrlCdr);

public record CreateVentaDetailDto(
    Guid LoteId,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal CostoHistorico,
    Guid? RecetaId = null);

public class CreateVentaCommandValidator : AbstractValidator<CreateVentaCommand>
{
    public CreateVentaCommandValidator()
    {
        _ = RuleFor(v => v.Detalles)
            .NotEmpty().WithMessage("A sale must have at least one detail.");

        _ = RuleForEach(v => v.Detalles).ChildRules(detail =>
        {
            _ = detail.RuleFor(d => d.LoteId).NotEmpty();
            _ = detail.RuleFor(d => d.Cantidad).GreaterThan(0);
            _ = detail.RuleFor(d => d.PrecioUnitario).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateVentaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CreateVentaCommand, Result<VentaCreadaDto>>
{
    public async Task<Result<VentaCreadaDto>> Handle(CreateVentaCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId) || string.IsNullOrEmpty(currentUser.Id))
        {
            return Result.Failure<VentaCreadaDto>(Error.Unauthorized("Venta.Auth", "User must be authenticated and assigned to a branch."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out var sucursalId))
        {
            return Result.Failure<VentaCreadaDto>(Error.Validation("Venta.Sucursal", "Invalid Sucursal ID in user context."));
        }

        if (!Guid.TryParse(currentUser.Id, out var empleadoId))
        {
            return Result.Failure<VentaCreadaDto>(Error.Validation("Venta.Empleado", "Invalid Empleado ID in user context."));
        }

        // 1. Validar que la Sesión de Caja esté abierta
        if (request.SesionId == null)
        {
            return Result.Failure<VentaCreadaDto>(Error.Validation("Venta.Caja", "La sesión de caja es requerida para procesar la venta."));
        }

        var sesionCaja = await context.POSSesionesCaja
            .FirstOrDefaultAsync(x => x.Id == request.SesionId && !x.IsDeleted, cancellationToken);

        if (sesionCaja == null || sesionCaja.EstadoSesion != EstadoSesion.Abierta)
        {
            return Result.Failure<VentaCreadaDto>(Error.Validation("Venta.Caja", "La sesión de caja no está abierta o no existe."));
        }

        List<DetalleVenta> detallesVenta = [];

        // 2. Validar y preparar detalles
        foreach (var detailDto in request.Detalles)
        {
            // Validar si el lote está en CUARENTENA (DIGEMID)
            var enCuarentena = await context.DIGEMIDInventarioCuarentena
                .AnyAsync(q => q.LoteId == detailDto.LoteId && q.EstadoResolucion == "Retenido" && !q.IsDeleted, cancellationToken);

            if (enCuarentena)
            {
                return Result.Failure<VentaCreadaDto>(Error.Validation("Venta.Cuarentena", $"El lote {detailDto.LoteId} se encuentra retenido en cuarentena y no está permitido venderlo bajo ninguna circunstancia."));
            }

            // Validar existencia del lote y stock en la sucursal actual
            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detailDto.LoteId && x.SucursalId == sucursalId, cancellationToken);

            if (inventario == null)
            {
                return Result.Failure<VentaCreadaDto>(Error.NotFound("Venta.Lote", $"El lote {detailDto.LoteId} no existe en esta sucursal."));
            }

            if (inventario.CantidadFisica < detailDto.Cantidad)
            {
                return Result.Failure<VentaCreadaDto>(Error.Validation("Venta.Stock", $"Stock insuficiente para el lote {detailDto.LoteId}. Disponible: {inventario.CantidadFisica}"));
            }

            var detailResult = DetalleVenta.Create(
                detailDto.LoteId,
                detailDto.Cantidad,
                detailDto.PrecioUnitario,
                detailDto.CostoHistorico,
                detailDto.RecetaId);

            if (!detailResult.IsSuccess)
            {
                return Result.Failure<VentaCreadaDto>(detailResult.Error);
            }

            // 3. Descontar stock
            inventario.UpdateStock(inventario.CantidadFisica - detailDto.Cantidad);

            detallesVenta.Add(detailResult.Value);
        }

        // 4. Crear la venta
        var ventaResult = Venta.Create(
            sucursalId,
            empleadoId,
            request.ClienteId,
            request.SesionId,
            detallesVenta,
            request.Estado);

        if (!ventaResult.IsSuccess)
        {
            return Result.Failure<VentaCreadaDto>(ventaResult.Error);
        }

        _ = context.Ventas.Add(ventaResult.Value);

        ComprobanteEmitidoDto? dtoComprobante = null;

        // 5. Generar Comprobante SUNAT
        var serie = await context.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.SucursalId == sucursalId && s.TipoComprobante == TipoComprobante.Boleta && s.EstadoSerie == "Activa" && !s.IsDeleted, cancellationToken);

        if (serie == null)
        {
            // Crear una serie por defecto si no existe
            var newSerieResult = SUNATSerieFiscal.Create(sucursalId, TipoComprobante.Boleta, "B001", 0, "Activa");
            if (newSerieResult.IsSuccess)
            {
                serie = newSerieResult.Value;
                _ = context.SUNATSeriesFiscales.Add(serie);
            }
        }

        if (serie != null)
        {
            var correlativo = serie.CorrelativoActual + 1;
            _ = serie.Update(serie.SucursalId, serie.TipoComprobante, serie.PrefijoSerie, correlativo, serie.EstadoSerie);

            var total = ventaResult.Value.MontoTotalBruto;
            var gravado = total * 0.82m;
            var igv = total * 0.18m;

            var comprobanteResult = SUNATComprobanteEmitido.Create(
                ventaResult.Value.Id,
                serie.Id,
                correlativo,
                "1", // DNI
                "00000000",
                "CLIENTE EVENTUAL",
                gravado,
                0,
                igv,
                total,
                "HASH_SIMULATED_" + Guid.NewGuid().ToString("N")[..8],
                "Aceptado",
                $"/comprobantes/XML_{correlativo}.xml",
                $"/comprobantes/CDR_{correlativo}.xml",
                $"https://sunat.gob.pe/verificar/{serie.PrefijoSerie}-{correlativo}"
            );

            if (comprobanteResult.IsSuccess)
            {
                _ = context.SUNATComprobantesEmitidos.Add(comprobanteResult.Value);
                dtoComprobante = new ComprobanteEmitidoDto(
                    serie.TipoComprobante.ToString(),
                    $"{serie.PrefijoSerie}-{correlativo:D8}",
                    "Aceptado",
                    comprobanteResult.Value.UrlPublicaVerificacion,
                    comprobanteResult.Value.RutaArchivoXml,
                    comprobanteResult.Value.RutaArchivoCdr
                );
            }
        }

        // 6. Si hay seguro copago, procesar reclamo
        if (request.AseguradoraId != null && request.MontoCubiertoSeguro != null && detallesVenta.Count > 0)
        {
            var reclamoResult = VentaReclamoSeguro.Create(
                detallesVenta[0].Id,
                request.AseguradoraId.Value,
                request.MontoCubiertoSeguro.Value,
                ventaResult.Value.MontoTotalBruto - request.MontoCubiertoSeguro.Value,
                "Aprobado",
                "AUTH_" + Guid.NewGuid().ToString("N")[..8]
            );

            if (reclamoResult.IsSuccess)
            {
                _ = context.VentasReclamosSeguro.Add(reclamoResult.Value);
            }
        }

        // 7. Evento Outbox
        var outboxResult = SistemaOutboxEvento.Create(
            "VentaCompletada",
            JsonSerializer.Serialize(new { VentaId = ventaResult.Value.Id, SucursalId = sucursalId }),
            false,
            null,
            null);

        if (outboxResult.IsSuccess)
        {
            _ = context.SistemaOutboxEventos.Add(outboxResult.Value);
        }

        // 8. Guardar todo en una única transacción atómica
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new VentaCreadaDto(ventaResult.Value.Id, dtoComprobante), 201);
    }
}
