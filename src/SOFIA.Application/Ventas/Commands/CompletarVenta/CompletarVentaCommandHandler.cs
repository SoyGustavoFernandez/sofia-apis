using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Application.Ventas.Common;
using SOFIA.Application.Ventas.Events;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Commands.CompletarVenta;

public class CompletarVentaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CompletarVentaCommand, Result<VentaCreadaDto>>
{
    public async Task<Result<VentaCreadaDto>> Handle(CompletarVentaCommand request, CancellationToken cancellationToken)
    {
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(sucursalResult.Error);
        }

        var sucursalId = sucursalResult.Value;

        var venta = await context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == request.VentaId && !v.IsDeleted, cancellationToken);

        if (venta == null)
        {
            return Result.Failure<VentaCreadaDto>(Error.NotFound("Venta.Completar", "Sale not found."), 404);
        }

        if (venta.SucursalId != sucursalId)
        {
            return Result.Failure<VentaCreadaDto>(Error.Forbidden("Venta.Completar", "You do not have permission to complete sales from another branch."));
        }

        if (venta.Estado != EstadoVenta.Pendiente)
        {
            return Result.Failure<VentaCreadaDto>(Error.Validation("Venta.Completar", "Only a pending sale can be completed."));
        }

        var sesionResult = await AsignarSesionPropiaAsync(venta, sucursalId, cancellationToken);
        if (sesionResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(sesionResult.Error);
        }

        var coberturaResult = await VentaSeguroProcessor.ResolveCoberturaAsync(context, request.AseguradoraId, request.MontoCubiertoSeguro, cancellationToken);
        if (coberturaResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(coberturaResult.Error, coberturaResult.StatusCode);
        }

        if (request.Detalles != null)
        {
            var updateResult = await VentaDetalleUpdater.ReplaceAsync(context, venta, request.Detalles, request.ClienteId, sucursalId, cancellationToken);
            if (!updateResult.IsSuccess)
            {
                return Result.Failure<VentaCreadaDto>(updateResult.Error, updateResult.StatusCode);
            }
        }

        var pagosResult = VentaPagoFactory.Build(request.Pagos);
        if (pagosResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(pagosResult.Error);
        }

        var registrarPagosResult = venta.RegistrarPagos(pagosResult.Value!, coberturaResult.Value);
        if (!registrarPagosResult.IsSuccess)
        {
            return Result.Failure<VentaCreadaDto>(registrarPagosResult.Error);
        }

        // Venta was already tracked (fetched, not Added), so its new VentaPago children need an explicit Add
        // or EF's change tracker treats them as pre-existing rows to UPDATE instead of INSERT.
        foreach (var pago in pagosResult.Value!)
        {
            _ = context.VentasPagos.Add(pago);
        }

        var comprobanteResult = await VentaComprobanteGenerator.EmitirBoletaAsync(context, venta, sucursalId, cancellationToken);
        if (comprobanteResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(comprobanteResult.Error);
        }

        var dtoComprobante = comprobanteResult.Value;

        var seguroResult = VentaSeguroProcessor.Process(context, venta, request.AseguradoraId, request.MontoCubiertoSeguro);
        if (seguroResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(seguroResult.Error);
        }

        venta.AddDomainEvent(new VentaCompletadaEvent(venta.Id, sucursalId));

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new VentaCreadaDto(venta.Id, dtoComprobante));
    }

    // The payment is collected into the caller's own open drawer, whoever parked the sale
    private async Task<Result> AsignarSesionPropiaAsync(Venta venta, Guid sucursalId, CancellationToken cancellationToken)
    {
        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return Result.Failure(empleadoResult.Error);
        }

        var sesionCaja = await context.POSSesionesCaja
            .FirstOrDefaultAsync(s => s.EmpleadoId == empleadoResult.Value && s.SucursalId == sucursalId && s.EstadoSesion == EstadoSesion.Abierta && !s.IsDeleted, cancellationToken);

        return sesionCaja == null
            ? Result.Failure(Error.Validation("Venta.Caja.SinSesionAbierta", "You need an open cash register session to collect the sale."))
            : venta.AsignarSesion(sesionCaja.Id);
    }
}
