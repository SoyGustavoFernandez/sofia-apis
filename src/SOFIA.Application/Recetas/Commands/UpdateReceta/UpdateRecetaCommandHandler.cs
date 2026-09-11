using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Commands.UpdateReceta;

public class UpdateRecetaCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateRecetaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateRecetaCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Recetas
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("RecetaMedica.NotFound", "Receta médica not found."));
        }

        var updateResult = entity.Update(
            request.ClienteId,
            request.MedicoId,
            request.FechaExpedicion,
            request.RepeticionesMax,
            request.IndicacionesUso);

        if (updateResult.IsFailure)
        {
            return Result.Failure<Guid>(updateResult.Error);
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
