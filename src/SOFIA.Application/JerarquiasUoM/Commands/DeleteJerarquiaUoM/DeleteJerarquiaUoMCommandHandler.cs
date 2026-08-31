using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Commands.DeleteJerarquiaUoM;

public class DeleteJerarquiaUoMCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteJerarquiaUoMCommand, Result>
{
    public async Task<Result> Handle(DeleteJerarquiaUoMCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.JerarquiasUoM
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("JerarquiaUoM.NotFound", $"JerarquÃ­a with ID {request.Id} was not found."), 404);
        }

        _ = context.JerarquiasUoM.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}
