using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empleados.Commands.CreateEmpleado;

public record CreateEmpleadoCommand : ICommand<Guid>
{
    public Guid Sucursal_Base_ID { get; init; }
    public string Nombres { get; init; } = string.Empty;
    public string Apellido_Paterno { get; init; } = string.Empty;
    public string Apellido_Materno { get; init; } = string.Empty;
    public string? Licencia_Prof { get; init; }
    public byte[]? Huella_Biometrica { get; init; }
}

public class CreateEmpleadoCommandValidator : AbstractValidator<CreateEmpleadoCommand>
{
    public CreateEmpleadoCommandValidator()
    {
        _ = RuleFor(v => v.Sucursal_Base_ID)
            .NotEmpty().WithMessage("Sucursal Base ID is required.");

        _ = RuleFor(v => v.Nombres)
            .NotEmpty().WithMessage("Nombres is required.")
            .MaximumLength(75).WithMessage("Nombres must not exceed 75 characters.");

        _ = RuleFor(v => v.Apellido_Paterno)
            .NotEmpty().WithMessage("Apellido Paterno is required.")
            .MaximumLength(75).WithMessage("Apellido Paterno must not exceed 75 characters.");

        _ = RuleFor(v => v.Apellido_Materno)
            .NotEmpty().WithMessage("Apellido Materno is required.")
            .MaximumLength(75).WithMessage("Apellido Materno must not exceed 75 characters.");

        _ = RuleFor(v => v.Licencia_Prof)
            .MaximumLength(50).WithMessage("Licencia Prof must not exceed 50 characters.");
    }
}

public class CreateEmpleadoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateEmpleadoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        var result = Empleado.Create(
            request.Sucursal_Base_ID,
            request.Nombres,
            request.Apellido_Paterno,
            request.Apellido_Materno,
            request.Licencia_Prof,
            request.Huella_Biometrica);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.Empleados.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
