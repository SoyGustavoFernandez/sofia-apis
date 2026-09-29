using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Commands.CreateReceta;

public class CreateRecetaCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateRecetaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRecetaCommand request, CancellationToken cancellationToken)
    {
        // Tenant-filtered lookups: an id from another company must not be stored as a reference
        if (!await dbContext.Pacientes.AnyAsync(p => p.Id == request.ClienteId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Paciente.NotFound", "The specified patient does not exist."), 404);
        }

        if (!await dbContext.ProfesionalesSalud.AnyAsync(m => m.Id == request.MedicoId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("ProfesionalSalud.NotFound", "The specified health professional does not exist."), 404);
        }

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
