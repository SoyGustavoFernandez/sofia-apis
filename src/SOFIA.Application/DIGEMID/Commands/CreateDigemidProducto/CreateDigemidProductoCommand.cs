using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.DIGEMID.Commands.CreateDigemidProducto;

public record CreateDigemidProductoCommand(string CodProd, string NomProd, string? Concent, string? FormaFarmaceutica, string? Fraccion, string? RegistroSanitario, string? Titular, string Estado) : ICommand<Guid>;

public class CreateDigemidProductoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateDigemidProductoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDigemidProductoCommand request, CancellationToken cancellationToken)
    {
        var result = DigemidCatalogoProducto.Create(
            request.CodProd,
            request.NomProd,
            request.Concent,
            request.FormaFarmaceutica,
            request.Fraccion,
            request.RegistroSanitario,
            request.Titular,
            request.Estado);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.DigemidCatalogoProductos.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
