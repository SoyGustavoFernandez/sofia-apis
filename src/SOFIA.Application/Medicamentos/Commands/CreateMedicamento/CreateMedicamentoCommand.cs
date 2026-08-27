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
    Domain.Enums.CondicionVenta CondicionVenta) : IRequest<Result<Guid>>;

// Validator = pipeline fast-fail; entity method = domain invariant. Both layers are intentional.
public class CreateMedicamentoCommandValidator : AbstractValidator<CreateMedicamentoCommand>
{
    public CreateMedicamentoCommandValidator()
    {
        _ = RuleFor(v => v.CodigoNacional)
            .NotEmpty().WithMessage("Código Nacional is required.")
            .MaximumLength(Medicamento.CodigoNacionalMaxLength).WithMessage($"Código Nacional must not exceed {Medicamento.CodigoNacionalMaxLength} characters.");

        _ = RuleFor(v => v.NombreComercial)
            .NotEmpty().WithMessage("Nombre Comercial is required.")
            .MaximumLength(Medicamento.NombreComercialMaxLength).WithMessage($"Nombre Comercial must not exceed {Medicamento.NombreComercialMaxLength} characters.");

        _ = RuleFor(v => v.LaboratorioId)
            .NotEmpty().WithMessage("Laboratorio ID is required.");

        _ = RuleFor(v => v.UnidadBaseId)
            .NotEmpty().WithMessage("Unidad Base ID is required.");

        _ = RuleFor(v => v.CondicionVenta)
            .IsInEnum().WithMessage("Invalid Condición de Venta.");
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
