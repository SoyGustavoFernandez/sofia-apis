using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Common;
using SOFIA.Application.Ventas.Events;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Commands.CreateVenta;

public class CreateVentaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CreateVentaCommand, Result<VentaCreadaDto>>
{
    public async Task<Result<VentaCreadaDto>> Handle(CreateVentaCommand request, CancellationToken cancellationToken)
    {
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(sucursalResult.Error);
        }

        var sucursalId = sucursalResult.Value;

        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(empleadoResult.Error);
        }

        var empleadoId = empleadoResult.Value;

        var sesionResult = await ValidateSesionCajaAsync(request.SesionId, cancellationToken);
        if (sesionResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(sesionResult.Error);
        }

        var detallesResult = await VentaDetalleFactory.BuildAsync(context, request.Detalles, sucursalId, cancellationToken);
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

        ComprobanteEmitidoDto? dtoComprobante = null;

        // A Pendiente sale can be parked with no payment yet; it only gets a comprobante and flips to Completada once paid.
        if (request.Pagos.Count > 0)
        {
            var pagosResult = VentaPagoFactory.Build(request.Pagos);
            if (pagosResult.IsFailure)
            {
                return Result.Failure<VentaCreadaDto>(pagosResult.Error);
            }

            var registrarPagosResult = ventaResult.Value.RegistrarPagos(pagosResult.Value!, request.MontoCubiertoSeguro ?? 0);
            if (!registrarPagosResult.IsSuccess)
            {
                return Result.Failure<VentaCreadaDto>(registrarPagosResult.Error);
            }

            dtoComprobante = await VentaComprobanteGenerator.GenerateAsync(context, ventaResult.Value, sucursalId, cancellationToken);

            if (request.AseguradoraId != null && request.MontoCubiertoSeguro != null && detallesResult.Value!.Count > 0)
            {
                VentaSeguroProcessor.Process(context, detallesResult.Value![0].Id, request.AseguradoraId.Value, request.MontoCubiertoSeguro.Value, ventaResult.Value.MontoTotalBruto);
            }

            ventaResult.Value.AddDomainEvent(new VentaCompletadaEvent(ventaResult.Value.Id, sucursalId));
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new VentaCreadaDto(ventaResult.Value.Id, dtoComprobante), 201);
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

}
