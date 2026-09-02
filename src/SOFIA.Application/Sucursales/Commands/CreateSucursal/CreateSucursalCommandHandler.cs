using MediatR;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Sucursales.Commands.CreateSucursal;

public class CreateSucursalCommandHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<CreateSucursalCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSucursalCommand request, CancellationToken cancellationToken)
    {
        var empresaResult = currentUser.GetEmpresaId();
        if (empresaResult.IsFailure)
        {
            return Result.Failure<Guid>(empresaResult.Error);
        }

        var result = Sucursal.Create(
            request.Nombre,
            request.DireccionFisica,
            request.NumeroLicencia,
            request.GerenteId,
            empresaResult.Value);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.Sucursales.Add(result.Value);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
