using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Queries.GetAseguradoras;

public record GetAseguradorasQuery() : IRequest<Result<List<object>>>;

public class GetAseguradorasQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetAseguradorasQuery, Result<List<object>>>
{
    public async Task<Result<List<object>>> Handle(GetAseguradorasQuery request, CancellationToken cancellationToken) => Result.Success(new List<object>());
}
