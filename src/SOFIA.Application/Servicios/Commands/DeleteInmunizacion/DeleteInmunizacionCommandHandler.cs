using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Servicios.Commands.DeleteInmunizacion;

public class DeleteInmunizacionCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteInmunizacionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteInmunizacionCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ServiciosClinicosInmunizacion
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("NotFound", "Record not found."));
        }

        _ = context.ServiciosClinicosInmunizacion.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
