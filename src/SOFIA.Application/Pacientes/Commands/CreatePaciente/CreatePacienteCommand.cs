using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Pacientes.Commands.CreatePaciente;

public record CreatePacienteCommand(string DocIdentidadGub, string NombreApellidos, DateOnly FechaNacimiento, string? ContactoPrimario) : ICommand<Guid>;

public class CreatePacienteCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreatePacienteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePacienteCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Entities.PacienteCliente.Create(request.DocIdentidadGub, request.NombreApellidos, request.FechaNacimiento, request.ContactoPrimario);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.Pacientes.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
