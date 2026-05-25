using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Queries.GetProveedores;

public record GetProveedoresQuery() : IRequest<Result<List<object>>>;

public class GetProveedoresQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetProveedoresQuery, Result<List<object>>>
{
    public async Task<Result<List<object>>> Handle(GetProveedoresQuery request, CancellationToken cancellationToken) => Result.Success(new List<object>());
}
