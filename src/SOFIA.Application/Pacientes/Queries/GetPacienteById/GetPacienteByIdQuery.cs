using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Pacientes.Queries.GetPacienteById;

public record PacienteDto(Guid Id, string DocIdentidadGub, string NombreApellidos, DateOnly FechaNacimiento, string? ContactoPrimario);

public record GetPacienteByIdQuery(Guid Id) : IRequest<Result<PacienteDto>>;

public class GetPacienteByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPacienteByIdQuery, Result<PacienteDto>>
{
    public async Task<Result<PacienteDto>> Handle(GetPacienteByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<PacienteDto>(Error.NotFound("Paciente.NotFound", $"Paciente with ID {request.Id} was not found."), 404);
        }

        var dto = new PacienteDto(
            entity.Id,
            entity.DocIdentidadGub,
            entity.NombreApellidos,
            entity.FechaNacimiento,
            entity.ContactoPrimario);

        return Result.Success(dto);
    }
}
