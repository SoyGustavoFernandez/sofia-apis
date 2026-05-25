using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Application.POS.Queries.GetSesiones;

namespace SOFIA.Application.POS.Queries.GetSesionById;

public record GetSesionByIdQuery(Guid Id) : IRequest<Result<SesionResumenDto>>;

public class GetSesionByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetSesionByIdQuery, Result<SesionResumenDto>>
{
    public async Task<Result<SesionResumenDto>> Handle(GetSesionByIdQuery request, CancellationToken cancellationToken)
    {
        var sesion = await context.POSSesionesCaja
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.Id && !o.IsDeleted, cancellationToken);

        if (sesion == null)
        {
            return Result.Failure<SesionResumenDto>(Error.NotFound("SesionCaja.NotFound", "Sesion de Caja not found."));
        }

        var dto = new SesionResumenDto(
            sesion.Id,
            sesion.SucursalId,
            sesion.EmpleadoId,
            sesion.FechaHoraApertura,
            sesion.FechaHoraCierre,
            sesion.MontoAperturaEfectivo,
            sesion.MontoCierreCalculado,
            sesion.EstadoSesion);

        return Result.Success(dto);
    }
}
