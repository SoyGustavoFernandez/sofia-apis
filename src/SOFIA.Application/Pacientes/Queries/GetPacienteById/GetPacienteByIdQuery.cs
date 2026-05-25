using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Pacientes.Queries.GetPacienteById;

public record GetPacienteByIdQuery(Guid Id) : IRequest<Result<PacienteDto>>;

public class GetPacienteByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPacienteByIdQuery, Result<PacienteDto>>
{
    public async Task<Result<PacienteDto>> Handle(GetPacienteByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        return entity == null ? Result.Failure<PacienteDto>(Error.NotFound("NotFound", "No se encontró el registro.")) : Result.Success(new PacienteDto(entity.Id));
    }
}

public record PacienteDto(Guid Id);
