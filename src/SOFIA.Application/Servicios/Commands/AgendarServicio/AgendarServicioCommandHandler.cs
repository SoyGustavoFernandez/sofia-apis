using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Commands.AgendarServicio;

public class AgendarServicioCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<AgendarServicioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AgendarServicioCommand request, CancellationToken cancellationToken)
    {
        // Tenant-filtered lookups: an id from another company must not be stored as a reference
        if (!await dbContext.Pacientes.AnyAsync(p => p.Id == request.ClienteId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Paciente.NotFound", "The specified patient does not exist."), 404);
        }

        if (!await dbContext.Medicamentos.AnyAsync(m => m.Id == request.ProductoId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Medicamento.NotFound", "The specified product does not exist."), 404);
        }

        if (request.VentaId is { } ventaId && !await dbContext.Ventas.AnyAsync(v => v.Id == ventaId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Venta.NotFound", "La venta original no fue encontrada."), 404);
        }

        var createResult = Domain.Entities.ServicioAgenda.Create(request.ClienteId, request.ProductoId, request.VentaId, request.FechaHoraProgramada, request.EstadoCita);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.ServiciosAgenda.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
