using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Recetas.Queries.GetRecetaById;

public record GetRecetaByIdQuery(Guid Id) : IRequest<Result<RecetaDto>>;

public class GetRecetaByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRecetaByIdQuery, Result<RecetaDto>>
{
    public async Task<Result<RecetaDto>> Handle(GetRecetaByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Recetas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        return entity == null ? Result.Failure<RecetaDto>(Error.NotFound("NotFound", "Record not found.")) : Result.Success(new RecetaDto(entity.Id));
    }
}

public record RecetaDto(Guid Id);
