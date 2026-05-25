using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Recetas.Commands.DeleteReceta;

public record DeleteRecetaCommand(Guid Id) : IRequest<Result<Guid>>;

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

        // Entity framework interceptor or soft delete mechanism should handle this
        // but we will just manually soft delete if the property exists, else remove
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
