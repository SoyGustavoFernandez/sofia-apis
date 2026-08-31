using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Transferencias.Commands.CreateTransferencia;

public class CreateTransferenciaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CreateTransferenciaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateTransferenciaCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener usuario actual y su sucursal base
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId) || string.IsNullOrEmpty(currentUser.Id))
        {
            return Result.Failure<Guid>(Error.Unauthorized("Transferencia.Auth", "El usuario debe estar autenticado y asignado a una sucursal."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out var sucursalOrigenId))
        {
            return Result.Failure<Guid>(Error.Validation("Transferencia.SucursalOrigen", "Invalid origin branch ID."));
        }

        if (!Guid.TryParse(currentUser.Id, out var empleadoEmisorId))
        {
            return Result.Failure<Guid>(Error.Validation("Transferencia.EmpleadoEmisor", "Invalid sender employee ID."));
        }

        // 2. Validar que origen y destino sean distintos
        if (sucursalOrigenId == request.SucursalDestinoId)
        {
            return Result.Failure<Guid>(Error.Validation("Transferencia.SucursalDestino", "La sucursal de destino debe ser diferente de la sucursal de origen."));
        }

        // 3. Validar existencia de sucursal de destino
        var destinoExists = await context.Sucursales.AnyAsync(s => s.Id == request.SucursalDestinoId, cancellationToken);
        if (!destinoExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Transferencia.SucursalDestinoNotFound", "La sucursal de destino especificada no existe."));
        }

        List<DetalleTransferencia> detallesTransferencia = [];

        // 4. Validar lotes y stock en la sucursal de origen
        foreach (var detailDto in request.Detalles)
        {
            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detailDto.LoteId && x.SucursalId == sucursalOrigenId, cancellationToken);

            if (inventario == null)
            {
                return Result.Failure<Guid>(Error.NotFound("Transferencia.LoteNotFound", $"El lote {detailDto.LoteId} no se encuentra registrado en la sucursal de origen."));
            }

            if (inventario.CantidadFisica < detailDto.CantidadEnviada)
            {
                return Result.Failure<Guid>(Error.Validation("Transferencia.StockInsuficiente", $"Stock insuficiente para el lote {detailDto.LoteId} en la sucursal de origen. Disponible: {inventario.CantidadFisica}"));
            }

            var detailResult = DetalleTransferencia.Create(detailDto.LoteId, detailDto.CantidadEnviada);
            if (detailResult.IsFailure)
            {
                return Result.Failure<Guid>(detailResult.Error);
            }

            detallesTransferencia.Add(detailResult.Value!);
        }

        // 5. Crear entidad Transferencia
        var transferenciaResult = Transferencia.Create(
            sucursalOrigenId,
            request.SucursalDestinoId,
            empleadoEmisorId,
            detallesTransferencia);

        if (transferenciaResult.IsFailure)
        {
            return Result.Failure<Guid>(transferenciaResult.Error);
        }

        _ = context.Transferencias.Add(transferenciaResult.Value!);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(transferenciaResult.Value!.Id, 201);
    }
}
