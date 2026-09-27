using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Application.Ventas.Common;
using SOFIA.Application.Ventas.Events;
using SOFIA.Domain.Common;
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
            return Result.Failure<VentaCreadaDto>(Error.NotFound("Venta.Completar", "Sale not found."));
        }

        if (venta.SucursalId != sucursalId)
        {
            return Result.Failure<VentaCreadaDto>(Error.Forbidden("Venta.Completar", "You do not have permission to complete sales from another branch."));
        }

        if (venta.Estado != EstadoVenta.Pendiente)
        {
            return Result.Failure<VentaCreadaDto>(Error.Validation("Venta.Completar", "Only a pending sale can be completed."));
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
                return Result.Failure<VentaCreadaDto>(updateResult.Error);
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

        var dtoComprobante = await VentaComprobanteGenerator.GenerateAsync(context, venta, sucursalId, cancellationToken);

        var seguroResult = VentaSeguroProcessor.Process(context, venta, request.AseguradoraId, request.MontoCubiertoSeguro);
        if (seguroResult.IsFailure)
        {
            return Result.Failure<VentaCreadaDto>(seguroResult.Error);
        }

        venta.AddDomainEvent(new VentaCompletadaEvent(venta.Id, sucursalId));

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new VentaCreadaDto(venta.Id, dtoComprobante));
    }
}
