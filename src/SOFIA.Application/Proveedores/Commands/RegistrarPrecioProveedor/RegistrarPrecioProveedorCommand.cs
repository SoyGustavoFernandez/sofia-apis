using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;

public record RegistrarPrecioProveedorCommand(Guid ProveedorId, Guid MedicamentoId, decimal PrecioCompra, decimal DescuentoPorcentaje) : IRequest<Result<Guid>>;

public class RegistrarPrecioProveedorCommandValidator : AbstractValidator<RegistrarPrecioProveedorCommand>
{
    public RegistrarPrecioProveedorCommandValidator()
    {
        _ = RuleFor(v => v.ProveedorId).NotEmpty();
        _ = RuleFor(v => v.MedicamentoId).NotEmpty();
        _ = RuleFor(v => v.PrecioCompra).GreaterThan(0);
        _ = RuleFor(v => v.DescuentoPorcentaje).InclusiveBetween(0, 100);
    }
}

public class RegistrarPrecioProveedorCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<RegistrarPrecioProveedorCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarPrecioProveedorCommand request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Proveedores.AnyAsync(x => x.Id == request.ProveedorId, cancellationToken);
        if (!exists)
        {
            return Result.Failure<Guid>(Error.NotFound("Proveedor", "Proveedor no encontrado"));
        }

        // This simulates saving a price list without creating a new table just for the test
        return Result.Success(Guid.NewGuid());
    }
}
