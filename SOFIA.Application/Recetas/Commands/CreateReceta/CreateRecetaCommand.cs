using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Commands.CreateReceta;

public record CreateRecetaCommand(Guid ClienteId, Guid MedicoId, DateOnly FechaExpedicion, int RepeticionesMax = 0, string? IndicacionesUso = null) : IRequest<Result<Guid>>;

public class CreateRecetaCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateRecetaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRecetaCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Entities.RecetaMedica.Create(request.ClienteId, request.MedicoId, request.FechaExpedicion, request.RepeticionesMax, request.IndicacionesUso);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.Recetas.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
