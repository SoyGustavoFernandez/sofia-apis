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

        var sesionResult = await ValidateClienteYSesionAsync(request, empleadoId, sucursalId, cancellationToken);
        if (sesionResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(sesionResult.Error, sesionResult.StatusCode);
        }

        var coberturaResult = await VentaSeguroProcessor.ResolveCoberturaAsync(context, request.AseguradoraId, request.MontoCubiertoSeguro, cancellationToken);
        if (coberturaResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(coberturaResult.Error, coberturaResult.StatusCode);
        }

        var exigirRegistroReceta = await VentaDetalleFactory.ExigeRegistroRecetaAsync(context, sucursalId, cancellationToken);

        var detallesResult = await VentaDetalleFactory.BuildAsync(context, request.Detalles, sucursalId, request.ClienteId, null, exigirRegistroReceta, cancellationToken);
        if (detallesResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(detallesResult.Error);
        }

        var ventaResult = Venta.Create(sucursalId, empleadoId, request.ClienteId, request.SesionId, detallesResult.Value!.Detalles, request.Estado);
        if (!ventaResult.IsSuccess)
        {
            return Result.Failure<VentaCreadaDto>(ventaResult.Error);
        }

        _ = context.Ventas.Add(ventaResult.Value);

        ComprobanteEmitidoDto? dtoComprobante = null;

        // A Pendiente sale can be parked with no payment yet; it only gets a comprobante and flips to Completada once paid.
        if (request.Pagos.Count > 0)
        {
            var cobroResult = await CobrarAsync(ventaResult.Value, request, coberturaResult.Value, detallesResult.Value.RequiereVerificacionReceta, cancellationToken);
            if (cobroResult.IsFailure)
            {
                return Result.Failure<VentaCreadaDto>(cobroResult.Error);
            }

            dtoComprobante = cobroResult.Value;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new VentaCreadaDto(ventaResult.Value.Id, dtoComprobante), 201);
    }

    // Charging needs the prescription confirmation when a line was sold without a registered prescription; parking does not
    private async Task<Result<ComprobanteEmitidoDto>> CobrarAsync(Venta venta, CreateVentaCommand request, decimal montoCubiertoSeguro, bool requiereVerificacionReceta, CancellationToken cancellationToken)
    {
        var verificacionResult = VentaRecetaVerificacion.Registrar(venta, requiereVerificacionReceta, request.RecetaVerificada, venta.EmpleadoId);
        if (verificacionResult.IsFailure)
        {
            return Result.Failure<ComprobanteEmitidoDto>(verificacionResult.Error);
        }

        var pagosResult = VentaPagoFactory.Build(request.Pagos);
        if (pagosResult.IsFailure)
        {
            return Result.Failure<ComprobanteEmitidoDto>(pagosResult.Error);
        }

        var registrarPagosResult = venta.RegistrarPagos(pagosResult.Value!, montoCubiertoSeguro);
        if (!registrarPagosResult.IsSuccess)
        {
            return Result.Failure<ComprobanteEmitidoDto>(registrarPagosResult.Error);
        }

        var comprobanteResult = await VentaComprobanteGenerator.EmitirBoletaAsync(context, venta, venta.SucursalId, cancellationToken);
        if (comprobanteResult.IsFailure)
        {
            return comprobanteResult;
        }

        var seguroResult = VentaSeguroProcessor.Process(context, venta, request.AseguradoraId, request.MontoCubiertoSeguro);
        if (seguroResult.IsFailure)
        {
            return Result.Failure<ComprobanteEmitidoDto>(seguroResult.Error);
        }

        venta.AddDomainEvent(new VentaCompletadaEvent(venta.Id, venta.SucursalId));

        return comprobanteResult;
    }

    // Tenant-filtered lookup: a patient of another company must not be stored as the customer
    private async Task<Result> ValidateClienteYSesionAsync(CreateVentaCommand request, Guid empleadoId, Guid sucursalId, CancellationToken cancellationToken)
    {
        if (request.ClienteId is { } clienteId && !await context.Pacientes.AnyAsync(p => p.Id == clienteId, cancellationToken))
        {
            return Result.Failure(Error.NotFound("Paciente.NotFound", "The specified patient does not exist."), 404);
        }

        return await ValidateSesionCajaAsync(request.SesionId, empleadoId, sucursalId, cancellationToken);
    }

    private async Task<Result> ValidateSesionCajaAsync(Guid? sesionId, Guid empleadoId, Guid sucursalId, CancellationToken cancellationToken)
    {
        if (sesionId == null)
        {
            return Result.Failure(Error.Validation("Venta.Caja", "A cash register session is required to process the sale."));
        }

        var sesionCaja = await context.POSSesionesCaja
            .FirstOrDefaultAsync(x => x.Id == sesionId && !x.IsDeleted, cancellationToken);

        if (sesionCaja == null || sesionCaja.EstadoSesion != EstadoSesion.Abierta)
        {
            return Result.Failure(Error.Validation("Venta.Caja", "The cash register session is not open or does not exist."));
        }

        // The sale's cash must land in the caller's own drawer at their current branch
        return sesionCaja.EmpleadoId != empleadoId || sesionCaja.SucursalId != sucursalId
            ? Result.Failure(Error.Forbidden("Venta.Caja.NoPropia", "The cash register session belongs to another cashier or branch."), 403)
            : Result.Success();
    }

}
