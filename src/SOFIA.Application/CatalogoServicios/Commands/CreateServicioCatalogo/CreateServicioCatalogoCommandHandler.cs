using MediatR;
using SOFIA.Application.CatalogoServicios.Common;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.CatalogoServicios.Commands.CreateServicioCatalogo;

public class CreateServicioCatalogoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateServicioCatalogoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateServicioCatalogoCommand request, CancellationToken cancellationToken)
    {
        var result = ServicioCatalogo.Create(request.Nombre, request.Precio, request.AplicaMedicamento, request.RegistraResultado);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        var duplicado = await ServicioCatalogoDuplicateChecker.FindAsync(context, null, result.Value!.Nombre, cancellationToken);
        if (duplicado is not null)
        {
            return Result.Failure<Guid>(duplicado, 409);
        }

        _ = context.ServiciosCatalogo.Add(result.Value);
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(result.Value.Id, 201);
    }
}
