using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Seguros.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Commands.UpdateAseguradora;

public class UpdateAseguradoraCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateAseguradoraCommand, Result>
{
    public async Task<Result> Handle(UpdateAseguradoraCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Aseguradoras
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Aseguradora.NotFound", $"Aseguradora with ID {request.Id} was not found."), 404);
        }

        var duplicado = await AseguradoraDuplicateChecker.FindAsync(context, entity.Id, request.CodigoIdentificadorNacional, cancellationToken);
        if (duplicado is not null)
        {
            return Result.Failure(duplicado, 409);
        }

        var result = entity.Update(request.NombreComercial, request.CodigoIdentificadorNacional);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
