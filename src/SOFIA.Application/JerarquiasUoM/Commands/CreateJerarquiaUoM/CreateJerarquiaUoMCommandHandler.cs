using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.JerarquiasUoM.Commands.CreateJerarquiaUoM;

public class CreateJerarquiaUoMCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateJerarquiaUoMCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateJerarquiaUoMCommand request, CancellationToken cancellationToken)
    {
        var result = JerarquiaUoM.Create(
            request.ProductoId,
            request.UnidadMayorId,
            request.UnidadMenorId,
            request.Multiplicador);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.JerarquiasUoM.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
