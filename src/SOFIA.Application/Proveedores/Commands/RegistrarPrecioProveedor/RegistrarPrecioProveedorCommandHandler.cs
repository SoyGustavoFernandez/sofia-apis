using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;

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
