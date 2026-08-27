using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Recetas.Commands.DeleteReceta;

public record DeleteRecetaCommand(Guid Id) : ICommand<Guid>;

public class DeleteRecetaCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteRecetaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteRecetaCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Recetas
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("NotFound", "No se encontró el registro."));
        }

        _ = context.Recetas.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
