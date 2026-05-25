using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Pacientes.Queries.GetPacientes;

public record GetPacientesQuery() : IRequest<Result<List<object>>>;

public class GetPacientesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetPacientesQuery, Result<List<object>>>
{
    public async Task<Result<List<object>>> Handle(GetPacientesQuery request, CancellationToken cancellationToken) => Result.Success(new List<object>());
}
