using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Servicios.Queries.GetServicioById;

public class GetServicioByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetServicioByIdQuery, Result<ServicioDto>>
{
    public async Task<Result<ServicioDto>> Handle(GetServicioByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.ServiciosAgenda
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        return entity == null ? Result.Failure<ServicioDto>(Error.NotFound("NotFound", "Record not found.")) : Result.Success(new ServicioDto(entity.Id));
    }
}
