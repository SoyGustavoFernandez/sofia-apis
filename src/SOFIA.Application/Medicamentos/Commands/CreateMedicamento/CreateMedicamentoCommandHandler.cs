using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Commands.CreateMedicamento;

public class CreateMedicamentoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateMedicamentoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateMedicamentoCommand request, CancellationToken cancellationToken)
    {
        var result = Medicamento.Create(
            request.CodigoNacional,
            request.NombreComercial,
            request.LaboratorioId,
            request.UnidadBaseId,
            request.CondicionVenta,
            request.PrecioVentaBase);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        var duplicado = await context.Medicamentos
            .AnyAsync(m => m.CodigoNacional == request.CodigoNacional && !m.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure<Guid>(Error.Conflict("Medicamento.CodigoNacional.Duplicado", "Another product already uses this national code."), 409);
        }

        // Tenant-filtered lookups: an id from another company must not be stored as a reference
        if (!await context.Laboratorios.AnyAsync(l => l.Id == request.LaboratorioId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Laboratorio.NotFound", "The specified laboratory does not exist."), 404);
        }

        if (!await context.UnidadesMedida.AnyAsync(u => u.Id == request.UnidadBaseId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("UnidadMedida.NotFound", "The specified unit of measure does not exist."), 404);
        }

        _ = context.Medicamentos.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
