using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Delivery.Commands.DeleteDespachoDelivery;

public class DeleteDespachoDeliveryCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteDespachoDeliveryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteDespachoDeliveryCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.DespachosDelivery
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("NotFound", "Record not found."));
        }

        _ = context.DespachosDelivery.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
