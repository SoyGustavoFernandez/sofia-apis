using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Pacientes.Commands.UpdatePaciente;

public record UpdatePacienteCommand(Guid Id) : ICommand<Guid>; // TODO: Add properties manually

public class UpdatePacienteCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdatePacienteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdatePacienteCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Pacientes
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("NotFound", "Record not found."));
        }

        // TODO: Update properties here

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
