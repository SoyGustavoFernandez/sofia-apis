using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Commands.UpdateProveedor;

public class UpdateProveedorCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateProveedorCommand, Result>
{
    public async Task<Result> Handle(UpdateProveedorCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Proveedores
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Proveedor.NotFound", $"Proveedor with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(request.RazonSocial, request.TaxId, request.TerminosFinancieros, request.CalificacionEsg, request.TasaCumplimiento);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
