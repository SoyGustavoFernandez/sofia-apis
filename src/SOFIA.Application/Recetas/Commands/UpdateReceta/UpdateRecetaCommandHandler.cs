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
            return Result.Failure<Guid>(Error.NotFound("RecetaMedica.NotFound", "Receta médica not found."), 404);
        }

        // Tenant-filtered lookups: an id from another company must not be stored as a reference
        if (entity.ClienteId != request.ClienteId
            && !await context.Pacientes.AnyAsync(p => p.Id == request.ClienteId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Paciente.NotFound", "The specified patient does not exist."), 404);
        }

        if (entity.MedicoId != request.MedicoId
            && !await context.ProfesionalesSalud.AnyAsync(m => m.Id == request.MedicoId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("ProfesionalSalud.NotFound", "The specified health professional does not exist."), 404);
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
