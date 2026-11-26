using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Seguros.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Commands.CreateAseguradora;

public class CreateAseguradoraCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateAseguradoraCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAseguradoraCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Entities.AseguradoraMedica.Create(request.NombreComercial, request.CodigoIdentificadorNacional);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        var duplicado = await AseguradoraDuplicateChecker.FindAsync(dbContext, null, request.CodigoIdentificadorNacional, cancellationToken);
        if (duplicado is not null)
        {
            return Result.Failure<Guid>(duplicado, 409);
        }

        _ = dbContext.Aseguradoras.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
