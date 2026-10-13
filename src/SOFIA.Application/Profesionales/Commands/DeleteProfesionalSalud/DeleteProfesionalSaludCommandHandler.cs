using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Commands.DeleteProfesionalSalud;

public class DeleteProfesionalSaludCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteProfesionalSaludCommand, Result>
{
    public async Task<Result> Handle(DeleteProfesionalSaludCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ProfesionalesSalud
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("ProfesionalSalud.NotFound", $"ProfesionalSalud with ID {request.Id} was not found."), 404);
        }

        var referenciadoPorRecetas = await context.Recetas
            .AnyAsync(r => r.MedicoId == request.Id && !r.IsDeleted, cancellationToken);
        if (referenciadoPorRecetas)
        {
            return Result.Failure(
                Error.Conflict("ProfesionalSalud.InUse", "Cannot delete a health professional referenced by prescriptions."),
                409);
        }

        _ = context.ProfesionalesSalud.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}
