using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Transferencias.Queries.GetTransferenciaById;

public record GetTransferenciaByIdQuery(Guid Id) : IRequest<Result<TransferenciaDto>>;

public class GetTransferenciaByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetTransferenciaByIdQuery, Result<TransferenciaDto>>
{
    public async Task<Result<TransferenciaDto>> Handle(GetTransferenciaByIdQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId))
        {
            return Result.Failure<TransferenciaDto>(Error.Unauthorized("Transferencia.Auth", "Usuario no autenticado."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out var userSucursalId))
        {
            return Result.Failure<TransferenciaDto>(Error.Validation("Transferencia.Auth", "Invalid user branch ID."));
        }

        var entity = await context.Transferencias
            .AsNoTracking()
            .Include(t => t.SucursalOrigen)
            .Include(t => t.SucursalDestino)
            .Include(t => t.EmpleadoEmisor)
            .Include(t => t.EmpleadoReceptor)
            .Include(t => t.Detalles)
                .ThenInclude(d => d.Lote)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<TransferenciaDto>(Error.NotFound("Transferencia.NotFound", $"La transferencia con ID {request.Id} no existe."), 404);
        }

        // Verify the transfer belongs to the user's branch (origin or destination)
        if (entity.SucursalOrigenId != userSucursalId && entity.SucursalDestinoId != userSucursalId)
        {
            return Result.Failure<TransferenciaDto>(Error.Forbidden("Transferencia.Forbidden", "No tiene permisos para acceder a esta transferencia ya que no pertenece a la sucursal de origen ni destino."));
        }

        List<DetalleTransferenciaDto> detallesDto = [.. entity.Detalles.Select(d => new DetalleTransferenciaDto(
            d.Id,
            d.LoteId,
            d.Lote?.NumeroLoteMfr ?? "Desconocido",
            d.CantidadEnviada,
            d.CantidadRecibida
        ))];

        var dto = new TransferenciaDto(
            entity.Id,
            entity.SucursalOrigenId,
            entity.SucursalOrigen?.Nombre ?? "Desconocida",
            entity.SucursalDestinoId,
            entity.SucursalDestino?.Nombre ?? "Desconocida",
            entity.EstadoLogistico.ToString(),
            entity.EmpleadoEmisorId,
            entity.EmpleadoEmisor != null ? $"{entity.EmpleadoEmisor.Nombres} {entity.EmpleadoEmisor.Apellido_Paterno}" : "Desconocido",
            entity.EmpleadoReceptorId,
            entity.EmpleadoReceptor != null ? $"{entity.EmpleadoReceptor.Nombres} {entity.EmpleadoReceptor.Apellido_Paterno}" : null,
            entity.FechaDespacho,
            entity.FechaRecepcion,
            detallesDto
        );

        return Result.Success(dto);
    }
}
