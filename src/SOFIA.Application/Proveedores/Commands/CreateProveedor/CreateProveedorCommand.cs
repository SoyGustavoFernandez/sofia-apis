using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Commands.CreateProveedor;

public record CreateProveedorCommand(string RazonSocial, string TaxId, string? TerminosFinancieros, decimal? CalificacionEsg, decimal TasaCumplimiento = 100.00m) : IRequest<Result<Guid>>;

public class CreateProveedorCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateProveedorCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProveedorCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Entities.ProveedorDistribuidor.Create(request.RazonSocial, request.TaxId, request.TerminosFinancieros, request.CalificacionEsg, request.TasaCumplimiento);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.Proveedores.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
