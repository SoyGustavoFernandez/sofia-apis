using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Commands.DeleteJerarquiaUoM;

public record DeleteJerarquiaUoMCommand(Guid Id) : IRequest<Result>;

public class DeleteJerarquiaUoMCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteJerarquiaUoMCommand, Result>
{
    public async Task<Result> Handle(DeleteJerarquiaUoMCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.JerarquiasUoM
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("JerarquiaUoM.NotFound", $"Jerarquía with ID {request.Id} was not found."), 404);
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}
