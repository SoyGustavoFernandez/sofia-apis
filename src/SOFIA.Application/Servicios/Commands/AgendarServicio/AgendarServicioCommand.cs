using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Commands.AgendarServicio;

public record AgendarServicioCommand(Guid ClienteId, Guid ProductoId, Guid? VentaId, DateTime FechaHoraProgramada, string EstadoCita = "Programada") : ICommand<Guid>;

public class AgendarServicioCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<AgendarServicioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AgendarServicioCommand request, CancellationToken cancellationToken)
    {
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
