using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

namespace SOFIA.Application.Devoluciones.Queries.GetDevolucionById;

public class GetDevolucionByIdQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess) : IRequestHandler<GetDevolucionByIdQuery, Result<DevolucionResumenDto>>
{
    public async Task<Result<DevolucionResumenDto>> Handle(GetDevolucionByIdQuery request, CancellationToken cancellationToken)
    {
        var allowed = await sucursalAccess.GetAllowedSucursalesAsync(cancellationToken);
        var devolucion = await context.Devoluciones
            .AsNoTracking()
            .WhereSucursalIn(d => context.SUNATComprobantesEmitidos.Where(c => c.Id == d.ComprobanteOrigenId).Select(c => c.Transaccion!.SucursalId).FirstOrDefault(), allowed)
            .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken);

        if (devolucion == null)
        {
            return Result.Failure<DevolucionResumenDto>(Error.NotFound("Devolucion.NotFound", "Devolucion not found."), 404);
        }

        var dto = new DevolucionResumenDto(
            devolucion.Id,
            devolucion.ComprobanteOrigenId,
            devolucion.EmpleadoAutorizaId,
            devolucion.FechaDevolucion,
            devolucion.MotivoSunatCatalogo,
            devolucion.SustentoDescriptivo);

        return Result.Success(dto);
    }
}
