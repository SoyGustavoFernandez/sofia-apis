using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Servicios.Queries.GetInmunizacionById;

public record GetInmunizacionByIdQuery(Guid Id) : IRequest<Result<InmunizacionDto>>;

public class GetInmunizacionByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetInmunizacionByIdQuery, Result<InmunizacionDto>>
{
    public async Task<Result<InmunizacionDto>> Handle(GetInmunizacionByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.ServiciosClinicosInmunizacion
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        return entity == null
            ? Result.Failure<InmunizacionDto>(Error.NotFound("NotFound", "Record not found."))
            : Result.Success(new InmunizacionDto(entity.Id));
    }
}

public record InmunizacionDto(Guid Id);
