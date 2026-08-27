using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Servicios.Commands.UpdateInmunizacion;

public record UpdateInmunizacionCommand(Guid Id) : ICommand<Guid>; // TODO: Add properties manually

public class UpdateInmunizacionCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateInmunizacionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateInmunizacionCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ServiciosClinicosInmunizacion
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("NotFound", "No se encontró el registro."));
        }

        // TODO: Update properties here

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
