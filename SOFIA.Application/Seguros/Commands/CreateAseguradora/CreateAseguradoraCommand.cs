using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Commands.CreateAseguradora;

public record CreateAseguradoraCommand(string NombreComercial, string CodigoIdentificadorNacional) : IRequest<Result<Guid>>;

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
        _ = dbContext.Aseguradoras.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
