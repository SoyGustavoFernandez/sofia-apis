using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Pacientes.Commands.DeletePaciente;

public record DeletePacienteCommand(Guid Id) : IRequest<Result<Guid>>;

public class DeletePacienteCommandHandler(IApplicationDbContext context) : IRequestHandler<DeletePacienteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeletePacienteCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Pacientes
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("NotFound", "No se encontró el registro."));
        }

        _ = context.Pacientes.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
