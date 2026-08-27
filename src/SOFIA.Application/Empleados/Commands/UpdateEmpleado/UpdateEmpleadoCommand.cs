using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empleados.Commands.UpdateEmpleado;

public record UpdateEmpleadoCommand : ICommand
{
    public Guid Id { get; init; }
    public Guid Sucursal_Base_ID { get; init; }
    public string Nombres { get; init; } = string.Empty;
    public string Apellido_Paterno { get; init; } = string.Empty;
    public string Apellido_Materno { get; init; } = string.Empty;
    public string? Licencia_Prof { get; init; }
    public byte[]? Huella_Biometrica { get; init; }
}

public class UpdateEmpleadoCommandValidator : AbstractValidator<UpdateEmpleadoCommand>
{
    public UpdateEmpleadoCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Id is required.");

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

public class UpdateEmpleadoCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateEmpleadoCommand, Result>
{
    public async Task<Result> Handle(UpdateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        var empleado = await context.Empleados
            .FindAsync([request.Id], cancellationToken);

        if (empleado is null)
        {
            return Result.Failure(Error.NotFound("Empleado.NotFound", $"Empleado with ID {request.Id} was not found."), 404);
        }

        var result = empleado.Update(
            request.Sucursal_Base_ID,
            request.Nombres,
            request.Apellido_Paterno,
            request.Apellido_Materno,
            request.Licencia_Prof,
            request.Huella_Biometrica);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
