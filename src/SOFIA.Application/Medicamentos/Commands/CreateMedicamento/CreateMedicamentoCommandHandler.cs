using FluentValidation;
using MediatR;
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

        _ = context.Medicamentos.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
