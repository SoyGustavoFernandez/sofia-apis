using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Seguros.Queries.GetAseguradoraById;

public record GetAseguradoraByIdQuery(Guid Id) : IRequest<Result<AseguradoraDto>>;

public class GetAseguradoraByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetAseguradoraByIdQuery, Result<AseguradoraDto>>
{
    public async Task<Result<AseguradoraDto>> Handle(GetAseguradoraByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Aseguradoras
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        return entity == null ? Result.Failure<AseguradoraDto>(Error.NotFound("NotFound", "Record not found.")) : Result.Success(new AseguradoraDto(entity.Id));
    }
}

public record AseguradoraDto(Guid Id);
