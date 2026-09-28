using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Delivery.Queries.GetDespachoDeliveryById;

public class GetDespachoDeliveryByIdQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess) : IRequestHandler<GetDespachoDeliveryByIdQuery, Result<DespachoDeliveryDto>>
{
    public async Task<Result<DespachoDeliveryDto>> Handle(GetDespachoDeliveryByIdQuery request, CancellationToken cancellationToken)
    {
        var allowed = await sucursalAccess.GetAllowedSucursalesAsync(cancellationToken);
        var entity = await context.DespachosDelivery
            .AsNoTracking()
            .WhereSucursalIn(x => x.Venta!.SucursalId, allowed)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        return entity == null
            ? Result.Failure<DespachoDeliveryDto>(Error.NotFound("NotFound", "Record not found."), 404)
            : Result.Success(new DespachoDeliveryDto(entity.Id));
    }
}
