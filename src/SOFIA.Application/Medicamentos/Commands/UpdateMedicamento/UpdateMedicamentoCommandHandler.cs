using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Commands.UpdateMedicamento;

public class UpdateMedicamentoCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateMedicamentoCommand, Result>
{
    public async Task<Result> Handle(UpdateMedicamentoCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Medicamentos
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Medicamento.NotFound", $"Medicamento with ID {request.Id} was not found."), 404);
        }

        var duplicado = await context.Medicamentos
            .AnyAsync(m => m.Id != entity.Id && m.CodigoNacional == request.CodigoNacional && !m.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure(Error.Conflict("Medicamento.CodigoNacional.Duplicado", "Another product already uses this national code."), 409);
        }

        // Tenant-filtered lookups: an id from another company must not be stored as a reference
        if (entity.LaboratorioId != request.LaboratorioId
            && !await context.Laboratorios.AnyAsync(l => l.Id == request.LaboratorioId, cancellationToken))
        {
            return Result.Failure(Error.NotFound("Laboratorio.NotFound", "The specified laboratory does not exist."), 404);
        }

        if (entity.UnidadBaseId != request.UnidadBaseId
            && !await context.UnidadesMedida.AnyAsync(u => u.Id == request.UnidadBaseId, cancellationToken))
        {
            return Result.Failure(Error.NotFound("UnidadMedida.NotFound", "The specified unit of measure does not exist."), 404);
        }

        var result = entity.Update(
            request.CodigoNacional,
            request.NombreComercial,
            request.LaboratorioId,
            request.UnidadBaseId,
            request.CondicionVenta,
            request.PrecioVentaBase);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
