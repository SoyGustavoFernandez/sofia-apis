using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Commands.DeleteLaboratorio;

public class DeleteLaboratorioCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteLaboratorioCommand, Result>
{
    public async Task<Result> Handle(DeleteLaboratorioCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Laboratorios
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Laboratorio.NotFound", $"Laboratorio with ID {request.Id} was not found."), 404);
        }

        var referenciadoPorMedicamentos = await context.Medicamentos
            .AnyAsync(m => m.LaboratorioId == request.Id && !m.IsDeleted, cancellationToken);
        if (referenciadoPorMedicamentos)
        {
            return Result.Failure(
                Error.Conflict("Laboratorio.InUse", "Cannot delete a laboratory referenced by medications."),
                409);
        }

        _ = context.Laboratorios.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}
