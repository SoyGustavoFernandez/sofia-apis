using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Commands.DeleteUnidadMedida;

public class DeleteUnidadMedidaCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteUnidadMedidaCommand, Result>
{
    public async Task<Result> Handle(DeleteUnidadMedidaCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.UnidadesMedida
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("UnidadMedida.NotFound", $"Unidad de Medida with ID {request.Id} was not found."), 404);
        }

        var referenciadaPorMedicamento = await context.Medicamentos
            .AnyAsync(m => m.UnidadBaseId == request.Id && !m.IsDeleted, cancellationToken);
        var referenciadaPorJerarquia = await context.JerarquiasUoM
            .AnyAsync(j => (j.UnidadMayorId == request.Id || j.UnidadMenorId == request.Id) && !j.IsDeleted, cancellationToken);
        if (referenciadaPorMedicamento || referenciadaPorJerarquia)
        {
            return Result.Failure(
                Error.Conflict("UnidadMedida.InUse", "Cannot delete a unit of measure referenced by medications or unit hierarchies."),
                409);
        }

        _ = context.UnidadesMedida.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}
