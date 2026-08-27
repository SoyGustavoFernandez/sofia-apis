using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Proveedores.Commands.DeleteProveedor;

public record DeleteProveedorCommand(Guid Id) : ICommand<Guid>;

public class DeleteProveedorCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteProveedorCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteProveedorCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Proveedores
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("NotFound", "No se encontró el registro."));
        }

        _ = context.Proveedores.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
