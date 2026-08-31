using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Pacientes.Commands.UpdatePaciente;

public record UpdatePacienteCommand(Guid Id, string DocIdentidadGub, string NombreApellidos, DateOnly FechaNacimiento, string? ContactoPrimario) : ICommand;

public class UpdatePacienteCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdatePacienteCommand, Result>
{
    public async Task<Result> Handle(UpdatePacienteCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Pacientes
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Paciente.NotFound", $"Paciente with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(request.DocIdentidadGub, request.NombreApellidos, request.FechaNacimiento, request.ContactoPrimario);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
