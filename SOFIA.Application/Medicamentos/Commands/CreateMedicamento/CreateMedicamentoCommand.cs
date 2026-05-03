using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Commands.CreateMedicamento;

public record CreateMedicamentoCommand(
    string CodigoNacional,
    string NombreComercial,
    Guid LaboratorioId,
    Guid UnidadBaseId,
    string CondicionVenta) : IRequest<Result<Guid>>;

public class CreateMedicamentoCommandValidator : AbstractValidator<CreateMedicamentoCommand>
{
    public CreateMedicamentoCommandValidator()
    {
        _ = RuleFor(v => v.CodigoNacional)
            .NotEmpty().WithMessage("Código Nacional is required.")
            .MaximumLength(50).WithMessage("Código Nacional must not exceed 50 characters.");

        _ = RuleFor(v => v.NombreComercial)
            .NotEmpty().WithMessage("Nombre Comercial is required.")
            .MaximumLength(150).WithMessage("Nombre Comercial must not exceed 150 characters.");

        _ = RuleFor(v => v.LaboratorioId)
            .NotEmpty().WithMessage("Laboratorio ID is required.");

        _ = RuleFor(v => v.UnidadBaseId)
            .NotEmpty().WithMessage("Unidad Base ID is required.");

        _ = RuleFor(v => v.CondicionVenta)
            .NotEmpty().WithMessage("Condición de Venta is required.")
            .Must(v => Medicamento.CondicionesValidas.Contains(v))
            .WithMessage($"Invalid Condición de Venta. Must be one of: {string.Join(", ", Medicamento.CondicionesValidas)}");
    }
}

public class CreateMedicamentoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateMedicamentoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateMedicamentoCommand request, CancellationToken cancellationToken)
    {
        var result = Medicamento.Create(
            request.CodigoNacional,
            request.NombreComercial,
            request.LaboratorioId,
            request.UnidadBaseId,
            request.CondicionVenta);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.Medicamentos.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
