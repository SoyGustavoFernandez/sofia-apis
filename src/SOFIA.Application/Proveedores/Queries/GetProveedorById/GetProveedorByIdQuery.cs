using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Proveedores.Queries.GetProveedorById;

public record GetProveedorByIdQuery(Guid Id) : IRequest<Result<ProveedorDto>>;

public class GetProveedorByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetProveedorByIdQuery, Result<ProveedorDto>>
{
    public async Task<Result<ProveedorDto>> Handle(GetProveedorByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        return entity == null ? Result.Failure<ProveedorDto>(Error.NotFound("NotFound", "Record not found.")) : Result.Success(new ProveedorDto(entity.Id));
    }
}

public record ProveedorDto(Guid Id);
