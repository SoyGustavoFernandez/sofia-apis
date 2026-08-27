using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Servicios.Commands.UpdateServicio;

public record UpdateServicioCommand(Guid Id) : ICommand<Guid>; // TODO: Add properties manually

public class UpdateServicioCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateServicioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateServicioCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ServiciosAgenda
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
