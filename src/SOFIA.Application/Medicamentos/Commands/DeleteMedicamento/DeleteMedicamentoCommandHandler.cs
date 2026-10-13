using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Medicamentos.Commands.DeleteMedicamento;

public class DeleteMedicamentoCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteMedicamentoCommand, Result>
{
    public async Task<Result> Handle(DeleteMedicamentoCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Medicamentos
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Medicamento.NotFound", $"Medicamento with ID {request.Id} was not found."), 404);
        }

        if (await IsReferencedAsync(request.Id, cancellationToken))
        {
            return Result.Failure(
                Error.Conflict("Medicamento.InUse", "Cannot delete a medication referenced by batches, presentations, unit hierarchies, clinical formulations, supplier prices, appointments, immunizations or compounding orders."),
                409);
        }

        _ = context.Medicamentos.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }

    private async Task<bool> IsReferencedAsync(Guid id, CancellationToken cancellationToken) =>
        await context.LotesInventario.AnyAsync(l => l.ProductoId == id && !l.IsDeleted, cancellationToken)
        || await context.PresentacionesVenta.AnyAsync(p => p.ProductoId == id && !p.IsDeleted, cancellationToken)
        || await context.JerarquiasUoM.AnyAsync(j => j.ProductoId == id && !j.IsDeleted, cancellationToken)
        || await context.FormulacionesClinicas.AnyAsync(f => f.ProductoId == id && !f.IsDeleted, cancellationToken)
        || await context.HistorialPreciosProveedor.AnyAsync(h => h.ProductoId == id && !h.IsDeleted, cancellationToken)
        || await context.ServiciosAgenda.AnyAsync(s => s.ProductoId == id && !s.IsDeleted, cancellationToken)
        || await context.ServiciosClinicosInmunizacion.AnyAsync(s => s.ProductoId == id && !s.IsDeleted, cancellationToken)
        || await context.MagistralesOrdenesProduccion.AnyAsync(m => m.ProductoResultanteId == id && !m.IsDeleted, cancellationToken);
}
