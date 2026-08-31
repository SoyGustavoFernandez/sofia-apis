using SOFIA.Domain.Enums;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Commands.RegistrarInmunizacion;

public class RegistrarInmunizacionCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<RegistrarInmunizacionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarInmunizacionCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Entities.ServicioClinicoInmunizacion.Create(request.VentaId, request.ClienteId, request.ProfesionalAdmnId, request.ProductoId, request.LoteId, request.ViaAdministracion, request.SitioAnatomico, request.VolumenDosis, request.FechaAdmnFisica, request.FechaEntregaVis, request.ModalidadRegistro);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.ServiciosClinicosInmunizacion.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
